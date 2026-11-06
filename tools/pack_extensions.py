# -*- coding: utf-8 -*-
"""打包 JianProject.CysTerra 三件套到统一目录 cysterra-Windows拓展/dist/。

用法（在 JianProject 仓库根）:
  python tools/pack_extensions.py

产出:
  ../dist/JianProject.CysTerra-<ver>.zip
  ../dist/JianProject.CysTerra.Features-<ver>.zip
  ../dist/JianProject.CysTerra.TTS-<ver>.zip

每个 zip 根目录须含: 主 dll、*.deps.json、Messages/、LICENSE.txt、COPYING.txt
"""
from __future__ import annotations

import re
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
# 统一压缩包目录：cysterra-Windows拓展/dist（与 Pancakes 等并列）
DIST = ROOT.parent / "dist"
NAMES = [
    "JianProject.CysTerra",
    "JianProject.CysTerra.Features",
    "JianProject.CysTerra.TTS",
]


def read_version() -> str:
    csproj = ROOT / "JianProject.CysTerra" / "JianProject.CysTerra.csproj"
    m = re.search(r"<Version>([^<]+)</Version>", csproj.read_text(encoding="utf-8"))
    if not m:
        raise SystemExit("cannot read Version from csproj")
    return m.group(1).strip()


def build(name: str) -> None:
    csproj = ROOT / name / f"{name}.csproj"
    r = subprocess.run(
        ["dotnet", "build", str(csproj), "-c", "Release"],
        cwd=str(ROOT),
        capture_output=True,
    )
    if r.returncode != 0:
        print((r.stdout or b"").decode("utf-8", "replace")[-2000:])
        print((r.stderr or b"").decode("utf-8", "replace")[-2000:])
        raise SystemExit(f"build failed: {name}")
    print("built", name)


def ext_outdir(name: str) -> Path:
    """优先 bin/ext/Release；须含 dll + Messages。"""
    base = ROOT / name / "bin"
    cands = []
    for dll in base.rglob(f"{name}.dll"):
        if "Release" not in dll.parts:
            continue
        d = dll.parent
        if not (d / "Messages").is_dir():
            continue
        score = 0
        if "ext" in d.parts:
            score += 10
        if (d / f"{name}.deps.json").is_file():
            score += 5
        cands.append((score, -dll.stat().st_mtime, d))
    if not cands:
        raise SystemExit(f"no Release output with Messages for {name}")
    cands.sort()
    return cands[-1][2]


def pack_one(name: str, ver: str) -> Path:
    out = ext_outdir(name)
    dll = out / f"{name}.dll"
    deps = out / f"{name}.deps.json"
    messages = out / "Messages"
    if not dll.is_file() or not deps.is_file() or not messages.is_dir():
        raise SystemExit(f"incomplete output under {out}")

    DIST.mkdir(parents=True, exist_ok=True)
    zip_path = DIST / f"{name}-{ver}.zip"
    if zip_path.exists():
        zip_path.unlink()

    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
        z.write(dll, dll.name)
        z.write(deps, deps.name)
        for f in sorted(messages.glob("*.json")):
            z.write(f, f"Messages/{f.name}")
        for lic in ("LICENSE.txt", "COPYING.txt"):
            src = out / lic
            if not src.is_file():
                src = ROOT / lic
            if src.is_file():
                z.write(src, lic)

    # 校验
    with zipfile.ZipFile(zip_path) as z:
        names = set(z.namelist())
    need = {dll.name, deps.name, "Messages/zh.json", "Messages/en.json"}
    missing = sorted(need - names)
    if missing:
        raise SystemExit(f"{zip_path.name} missing entries: {missing}")
    print("wrote", zip_path, zip_path.stat().st_size)
    return zip_path


def clear_legacy_jianproject_dist() -> None:
    """不再使用 JianProject/dist，避免与统一 dist 混淆。"""
    legacy = ROOT / "dist"
    if not legacy.is_dir():
        return
    for z in legacy.glob("JianProject.CysTerra*.zip"):
        z.unlink()
        print("removed legacy", z.relative_to(ROOT))
    # 若空则留着；有其它文件不删目录


def main() -> None:
    ver = read_version()
    print("version", ver)
    print("dist", DIST)
    for name in NAMES:
        build(name)
    for name in NAMES:
        pack_one(name, ver)
    clear_legacy_jianproject_dist()
    print("OK — import from:", DIST)


if __name__ == "__main__":
    main()
