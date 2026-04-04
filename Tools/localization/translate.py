import os
import re
import requests
import time

LOCALE_DIR = "../../Resources/Locale/ru-RU/_RMC14/"

# Словарь "ключ → желаемый перевод"
DICTIONARY = {
    "Ravager": "Разрушитель",
    "Pulse rifle": "Импульсная винтовка",
    "Marine": "Морпех",
    "Pistol": "Пистолет",
    "Helmet": "Шлем",
    "Armor": "Броня",
    # Имена ксеноморфов
    "Crusher": "Крушитель",
    "Praetorian": "Преторианец",
    "Queen": "Королева",
    "Runner": "Бегун",
    "Warrior": "Воин"
    # добавляй сюда любые другие термины
}

def apply_dictionary(text):
    """Заменяет слова по словарю"""
    for word, translation in DICTIONARY.items():
        # заменяем слово только целиком, игнорируем регистр
        text = re.sub(rf'\b{re.escape(word)}\b', translation, text, flags=re.IGNORECASE)
    return text

def translate(text):
    url = "https://translate.googleapis.com/translate_a/single"
    params = {
        "client": "gtx",
        "sl": "en",
        "tl": "ru",
        "dt": "t",
        "q": text
    }
    try:
        r = requests.get(url, params=params, timeout=10)
        return r.json()[0][0][0]
    except:
        return text

def protect_variables(text):
    variables = re.findall(r"\{.*?\}", text)
    protected = text
    for i, var in enumerate(variables):
        protected = protected.replace(var, f"__VAR{i}__")
    return protected, variables

def restore_variables(text, variables):
    for i, var in enumerate(variables):
        text = text.replace(f"__VAR{i}__", var)
    return text

def contains_cyrillic(text):
    return bool(re.search(r'[а-яА-ЯёЁ]', text))

def translate_ftl_file(path):
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    # Если хотя бы одна строка уже на русском — пропускаем весь файл
    if any(contains_cyrillic(line) for line in lines):
        print(f"⏭ Пропускаем уже переведённый файл: {path}")
        return

    new_lines = []

    for line in lines:
        if "=" not in line:
            new_lines.append(line)
            continue

        key, value = line.split("=", 1)
        value = value.strip()

        if not value:
            new_lines.append(line)
            continue

        # Защищаем переменные
        protected, vars = protect_variables(value)

        # Сначала применяем словарь
        protected = apply_dictionary(protected)

        # Затем машинный перевод
        translated = translate(protected)
        translated = restore_variables(translated, vars)

        new_line = f"{key}= {translated}\n"
        new_lines.append(new_line)

        time.sleep(0.05)

    with open(path, "w", encoding="utf-8") as f:
        f.writelines(new_lines)

def run():
    total = 0
    for root, dirs, files in os.walk(LOCALE_DIR):
        for file in files:
            if file.endswith(".ftl"):
                path = os.path.join(root, file)
                translate_ftl_file(path)
                total += 1
    print()
    print("✔ Файлов обработано:", total)

if __name__ == "__main__":
    run()
