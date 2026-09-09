"""Reviewed gameplay terminology (language order matches translate_catalog.py)."""
from localize_catalog import *
from translate_catalog import LANGUAGES

rows = [
('Лёгкое|Лёгкая', 'Easy|Leicht|Fácil|Facile|Facile|Fácil|Kolay|Mudah'),
('Среднее|Средняя', 'Medium|Mittel|Media|Media|Moyenne|Média|Orta|Sedang'),
('Сложное|Сложная', 'Hard|Schwer|Difícil|Difficile|Difficile|Difícil|Zor|Sulit'),
(' с', ' s| s| s| s| s| s| sn| dtk'),
(' м', ' m| m| m| m| m| m| m| m'),
('Забрать подарок', 'Claim gift|Geschenk abholen|Reclamar regalo|Ritira regalo|Récupérer le cadeau|Resgatar presente|Hediyeyi al|Ambil hadiah'),
('ПОЛУЧЕНО', 'CLAIMED|ABGEHOLT|RECLAMADO|RITIRATO|RÉCUPÉRÉ|RESGATADO|ALINDI|DIAMBIL'),
('Через ', 'In |In |En |Tra |Dans |Em |Kalan: |Dalam '),
('Спин за рекламу', 'Watch ad to spin|Werbung ansehen und drehen|Ver anuncio para girar|Guarda un annuncio per girare|Voir une pub pour tourner|Ver anúncio para girar|Reklam izle ve çevir|Tonton iklan untuk memutar'),
('Есть {0} / нужно {1}', 'Have {0} / need {1}|Vorhanden {0} / benötigt {1}|Tienes {0} / necesitas {1}|Disponibili {0} / richiesti {1}|Possédés {0} / requis {1}|Você tem {0} / precisa de {1}|Mevcut {0} / gereken {1}|Ada {0} / perlu {1}'),
('Создать ×{0} · {1} с', 'Craft ×{0} · {1} s|Herstellen ×{0} · {1} s|Crear ×{0} · {1} s|Crea ×{0} · {1} s|Fabriquer ×{0} · {1} s|Criar ×{0} · {1} s|Üret ×{0} · {1} sn|Buat ×{0} · {1} dtk'),
('Занято ячеек: ', 'Slots used: |Belegte Plätze: |Casillas ocupadas: |Slot occupati: |Emplacements occupés : |Espaços ocupados: |Dolu yuvalar: |Slot terisi: '),
('Чувствительность\nприближения', 'Zoom\nsensitivity|Zoom-\nEmpfindlichkeit|Sensibilidad\ndel zoom|Sensibilità\ndello zoom|Sensibilité\ndu zoom|Sensibilidade\ndo zoom|Yakınlaştırma\nhassasiyeti|Sensitivitas\nzoom'),
('Добыча: ', 'Loot: |Beute: |Botín: |Bottino: |Butin : |Saque: |Ganimet: |Jarahan: '),
(' к выходу каждого крафта', ' to each crafting output| pro Herstellung| por cada fabricación| per ogni creazione| par fabrication| por fabricação| her üretimde| per hasil pembuatan'),
('Общая громкость', 'Master volume|Gesamtlautstärke|Volumen general|Volume generale|Volume général|Volume geral|Ana ses düzeyi|Volume utama'),
('ОТКРЫВАЕТ', 'UNLOCKS|SCHALTET FREI|DESBLOQUEA|SBLOCCA|DÉBLOQUE|DESBLOQUEIA|KİLİDİNİ AÇAR|MEMBUKA'),
('Язык: ', 'Language: |Sprache: |Idioma: |Lingua: |Langue : |Idioma: |Dil: |Bahasa: '),
]
for column,name in enumerate(LANGUAGES):
    p=OUT/(name+'.json'); data=json.loads(read(p)); values={i['key']:i for i in data['items']}
    for keys,translations in rows:
        for key in keys.split('|'):
            if key in values: values[key]['value']=translations.split('|')[column]
    # Use the same terminology in the serialized initial counter.
    if 'Занято ячеек: 0' in values: values['Занято ячеек: 0']['value']=values['Занято ячеек: ']['value']+'0'
    write(p,json.dumps(data,ensure_ascii=False,indent=2)+'\n')
