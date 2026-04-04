import os
import re

LOCALE_DIR = "../../Resources/Locale/ru-RU"

NAME_PATTERN = re.compile(r"^(ent-[\w\d]+)-name\s*=\s*(.*)")
DESC_PATTERN = re.compile(r"^(ent-[\w\d]+)-desc\s*=\s*(.*)")
KEY_PATTERN = re.compile(r"^(ent-[\w\d]+)\s*=")

def read_ftl_file(path):
    """Читает файл и возвращает список строк"""
    with open(path, "r", encoding="utf-8") as f:
        return [line.rstrip() for line in f]

def write_ftl_file(path, lines):
    """Записывает строки в файл"""
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")

def fix_and_collect_keys(path, global_keys):
    """
    Исправляет старый формат и собирает ключи.
    Удаляет дубликаты ключей на основе global_keys.
    """
    lines = read_ftl_file(path)
    entries = {}
    other_lines = []
    removed_duplicates = 0

    for line in lines:
        if not line.strip():
            continue

        # Старый формат name/desc
        name_match = NAME_PATTERN.match(line)
        desc_match = DESC_PATTERN.match(line)
        if name_match:
            key, value = name_match.groups()
            entries.setdefault(key, {})["name"] = value
            continue
        if desc_match:
            key, value = desc_match.groups()
            entries.setdefault(key, {})["desc"] = value
            continue

        # Новый формат
        key_match = KEY_PATTERN.match(line)
        if key_match:
            key = key_match.group(1)
            if key in global_keys:
                removed_duplicates += 1
                continue
            global_keys.add(key)

        other_lines.append(line)

    # Создаем новый список строк
    new_lines = []
    for key, data in entries.items():
        if key in global_keys:
            # Если ключ уже в глобальном словаре, пропускаем
            removed_duplicates += 1
            continue
        global_keys.add(key)

        if "name" in data:
            new_lines.append(f"{key} = {data['name']}")
        if "desc" in data:
            new_lines.append(f"  .desc = {data['desc']}")
        new_lines.append("")

    new_lines.extend(other_lines)
    new_lines.append("")  # пустая строка в конце файла

    write_ftl_file(path, new_lines)
    return removed_duplicates, len(entries)

def run_global_fixer():
    global_keys = set()
    ftl_files = []

    # Составляем список всех .ftl файлов
    for root, dirs, files in os.walk(LOCALE_DIR):
        for file in files:
            if file.endswith(".ftl"):
                ftl_files.append(os.path.join(root, file))

    total_files = len(ftl_files)
    total_removed = 0
    total_entries = 0

    for i, path in enumerate(ftl_files, 1):
        removed, entries = fix_and_collect_keys(path, global_keys)
        total_removed += removed
        total_entries += entries
        percent = (i / total_files) * 100
        print(f"[{i}/{total_files}] {percent:.2f}% → {path}, entries: {entries}, removed duplicates: {removed}")

    print("\n✅ Глобальный FTL Fixer завершил работу")
    print("Файлов обработано:", total_files)
    print("Удалено дубликатов:", total_removed)
    print("Обработано записей:", total_entries)

if __name__ == "__main__":
    run_global_fixer()
