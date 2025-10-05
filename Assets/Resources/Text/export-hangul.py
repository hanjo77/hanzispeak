import re

def extract_unique_hangul(filename):
    # Unicode range: Hangul syllables (AC00–D7A3) + Hangul Jamo (1100–11FF, 3130–318F)
    hangul_regex = re.compile(r'[\u1100-\u11FF\u3130-\u318F\uAC00-\uD7A3]')
    
    unique_chars = set()

    with open(filename, 'r', encoding='utf-8') as file:
        for line in file:
            matches = hangul_regex.findall(line)
            unique_chars.update(matches)

    return ''.join(sorted(unique_chars))


if __name__ == "__main__":
    import sys
    if len(sys.argv) != 2:
        print("Usage: python extract_hangul.py <your_text_file.txt>")
    else:
        chars = extract_unique_hangul(sys.argv[1])
        print(f"\n🧵 Unique Hangul Characters Used ({len(chars)} total):\n")
        print(chars)
