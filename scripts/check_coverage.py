#!/usr/bin/env python3
"""
MultiGuide - Code Coverage Threshold Checker
Kiểm tra độ phủ mã nguồn (Code Coverage) của tầng MultiGuide.Business từ Cobertura XML.
Hỗ trợ tổng hợp (merge) dữ liệu từ nhiều test project trong solution (Business.Tests, Integration.Tests).
Tự động ngắt (exit code 1) nếu tỷ lệ bao phủ dòng (line coverage) dưới ngưỡng quy định.
"""

import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET

# Đảm bảo in UTF-8 an toàn trên mọi hệ điều hành (kể cả Windows console)
if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass


def find_all_cobertura_files(search_dir: str) -> list[str]:
    pattern = os.path.join(search_dir, "**", "coverage.cobertura.xml")
    files = glob.glob(pattern, recursive=True)
    return files


def check_coverage(cobertura_files: list[str], target_package: str, min_coverage: float) -> bool:
    print(f"Tổng hợp dữ liệu coverage từ {len(cobertura_files)} file:")
    for f in cobertura_files:
        print(f"  - {f}")

    # Cấu trúc lưu: package_name -> class_name -> line_number -> {'hits': max_hits, 'branch': bool, 'condition_coverage': str}
    line_hits: dict[str, dict[str, dict[str, int]]] = {}
    line_branches: dict[str, dict[str, dict[str, bool]]] = {}

    found_package = False

    for file_path in cobertura_files:
        try:
            tree = ET.parse(file_path)
            root = tree.getroot()
        except Exception as e:
            print(f"::warning::Không thể đọc file {file_path}: {e}")
            continue

        for pkg in root.findall(".//package"):
            pkg_name = pkg.attrib.get("name", "")
            if target_package.lower() not in pkg_name.lower():
                continue

            found_package = True
            if pkg_name not in line_hits:
                line_hits[pkg_name] = {}
                line_branches[pkg_name] = {}

            for cls in pkg.findall(".//class"):
                cls_name = cls.attrib.get("name", "")
                if cls_name not in line_hits[pkg_name]:
                    line_hits[pkg_name][cls_name] = {}
                    line_branches[pkg_name][cls_name] = {}

                for line in cls.findall(".//line"):
                    line_num = line.attrib.get("number")
                    hits = int(line.attrib.get("hits", 0))
                    is_branch = line.attrib.get("branch", "false").lower() == "true"

                    cur_hits = line_hits[pkg_name][cls_name].get(line_num, 0)
                    line_hits[pkg_name][cls_name][line_num] = max(cur_hits, hits)
                    if is_branch:
                        line_branches[pkg_name][cls_name][line_num] = True

    if not found_package:
        print(f"::error::Không tìm thấy package mục tiêu '{target_package}' trong các file coverage!")
        return False

    all_passed = True

    for pkg_name, classes in line_hits.items():
        total_lines = 0
        covered_lines = 0
        total_branches = 0
        covered_branches = 0

        for cls_name, lines in classes.items():
            for line_num, hits in lines.items():
                total_lines += 1
                if hits > 0:
                    covered_lines += 1
                    if line_branches[pkg_name][cls_name].get(line_num, False):
                        covered_branches += 1
                if line_branches[pkg_name][cls_name].get(line_num, False):
                    total_branches += 1

        line_rate = (covered_lines / total_lines * 100.0) if total_lines > 0 else 0.0
        branch_rate = (covered_branches / total_branches * 100.0) if total_branches > 0 else 100.0

        print("\n" + "=" * 65)
        print(f" CODE COVERAGE REPORT - {pkg_name}")
        print("=" * 65)
        print(f"  * Line Coverage   : {line_rate:6.2f}% ({covered_lines}/{total_lines} lines)")
        print(f"  * Branch Coverage : {branch_rate:6.2f}% ({covered_branches}/{total_branches} branch points)")
        print(f"  * Required Min    : {min_coverage:6.2f}%")
        print("=" * 65)

        if line_rate < min_coverage:
            print(f"::error::Coverage of {pkg_name} ({line_rate:.2f}%) is BELOW the required threshold ({min_coverage:.2f}%)!")
            all_passed = False
        else:
            print(f"[OK] Coverage of {pkg_name} ({line_rate:.2f}%) MEETS the threshold (>= {min_coverage:.2f}%).\n")

    return all_passed


def main():
    parser = argparse.ArgumentParser(description="MultiGuide Coverage Checker")
    parser.add_argument("--file", help="Path to a single coverage.cobertura.xml")
    parser.add_argument("--dir", default="TestResults", help="Directory to search for coverage.cobertura.xml files")
    parser.add_argument("--package", default="MultiGuide.Business", help="Target package name (default: MultiGuide.Business)")
    parser.add_argument("--min-coverage", type=float, default=60.0, help="Minimum line coverage percentage (default: 60.0)")

    args = parser.parse_args()

    if args.file:
        files = [args.file]
    else:
        files = find_all_cobertura_files(args.dir)

    if not files:
        print(f"::error::No coverage.cobertura.xml files found in directory '{args.dir}'!")
        sys.exit(1)

    passed = check_coverage(files, args.package, args.min_coverage)
    if not passed:
        sys.exit(1)

    sys.exit(0)


if __name__ == "__main__":
    main()
