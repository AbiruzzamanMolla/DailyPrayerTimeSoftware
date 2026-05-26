import json
import os
import concurrent.futures
from deep_translator import GoogleTranslator

target_langs = {'hi': 'hindi', 'ta': 'tamil', 'te': 'telugu', 'ml': 'malayalam', 'id': 'indonesian', 'ar': 'arabic'}

def translate_single(lang_code, text):
    try:
        # Create a new translator instance per thread to avoid state corruption
        translator = GoogleTranslator(source='en', target=lang_code)
        return translator.translate(text)
    except Exception as e:
        return text

def translate_for_lang(lang_code, keys, values):
    print(f"Translating {lang_code}...")
    
    with concurrent.futures.ThreadPoolExecutor(max_workers=10) as executor:
        # Pass lang_code to each call
        translated_values = list(executor.map(lambda x: translate_single(lang_code, x), values))
        
    out_data = {}
    for k, tv in zip(keys, translated_values):
        if tv:
            tv = tv.replace('{ 0 }', '{0}').replace('{ 1 }', '{1}').replace('{0 }', '{0}').replace('{ 0}', '{0}')
            tv = tv.replace('{ 1}', '{1}').replace('{1 }', '{1}')
            out_data[k] = tv
        else:
            out_data[k] = data[k] # fallback
            
    out_file = os.path.join('i18n', f'{lang_code}.json')
    with open(out_file, 'w', encoding='utf-8') as f:
        json.dump(out_data, f, ensure_ascii=False, indent=2)
    print(f"Finished {lang_code}.json")

def main():
    en_file = os.path.join('i18n', 'en.json')
    with open(en_file, 'r', encoding='utf-8') as f:
        global data
        data = json.load(f)
    
    keys = list(data.keys())
    values = [data[k] for k in keys]
    
    for lang_code in target_langs.keys():
        translate_for_lang(lang_code, keys, values)

if __name__ == '__main__':
    main()
