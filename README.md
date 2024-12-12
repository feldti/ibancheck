# ibancheck

## Commandline

Dieses Programm testet den Aufbau einer IBAN und gibt einen Status zurück, ib die IBAN einen gültign Uafbau besitzt bzw, welches Kreditinstitut dazu gehört.

Der Aufruf erfolgt via 

<pre>
ibanservice [iban].
</pre>

## HTTP-Service
Das Projekt "ibanhttpservice" stellt ibanservice als einfachen http-Dienst zur Verfügung. Start erfolgt hier mittels 

<pre>
ibanhttpservice [portnummer].
</pre>

## Bankinformationen

Damit die Informationen der entsprchenden Bank zurückgegeben werden, sollte man sich eine Datei "plz-aktuell-csv-data-csv" von der Bundesbank holen und sie mit der hier mitgelieferten Datei austauschen (https://www.bundesbank.de/de/aufgaben/unbarer-zahlungsverkehr/serviceangebot/bankleitzahlen/download-bankleitzahlen-602592).


## Hilfe

Das ist ein Quick-and-Dirty Projekt. Das alles ist mit Hilfe von ChatGPT entwickelt worden. Diente zum Testen der Programmierung von ChatGPT. Ursprünglich wollte ich eine Lösung für eine bash haben, aber mit dem Einsatz von swk und grep kommt auch chatgpt manchmal gut durcheinander. C# funktioniert viel besser.

## PostgreSQL

Und wenn man den reinen IBAN Check in der PostgreSQL haben möchte (Abfrage via SELECT validate_iban('DE89370400440532013000'); ). Auch das konnte chatgpt erzeugen:

<pre>
CREATE OR REPLACE FUNCTION validate_iban(iban_input TEXT)
RETURNS BOOLEAN AS $$
DECLARE
iban_cleaned TEXT;          -- Bereinigte IBAN ohne Leerzeichen
iban_rearranged TEXT;       -- IBAN mit verschobenen ersten vier Zeichen
iban_numeric TEXT;          -- IBAN als numerischer String (Buchstaben in Zahlen umgewandelt)
mod_result NUMERIC;         -- Ergebnis der Modulo-97-Berechnung
BEGIN
-- Entfernen von Leerzeichen und Großschreibung erzwingen
iban_cleaned := UPPER(REPLACE(iban_input, ' ', ''));

    -- Validieren der IBAN-Länge (Minimalanforderung: 15 Zeichen)
    IF LENGTH(iban_cleaned) < 15 THEN
        RETURN FALSE;
    END IF;

    -- Verschieben der ersten vier Zeichen ans Ende
    iban_rearranged := SUBSTRING(iban_cleaned FROM 5)  SUBSTRING(iban_cleaned FROM 1 FOR 4);

    -- Buchstaben in Zahlen umwandeln (A=10, ..., Z=35)
    iban_numeric := '';
    FOR i IN 1..LENGTH(iban_rearranged) LOOP
        IF SUBSTRING(iban_rearranged, i, 1) ~ '[A-Z]' THEN
            iban_numeric := iban_numeric  (ASCII(SUBSTRING(iban_rearranged, i, 1)) - 55); -- A=10, ..., Z=35
        ELSE
            iban_numeric := iban_numeric || SUBSTRING(iban_rearranged, i, 1);
        END IF;
    END LOOP;

    -- Modulo-97-Berechnung
    mod_result := MOD(CAST(iban_numeric AS NUMERIC), 97);

    -- Eine gültige IBAN ergibt bei der Modulo-97-Prüfung den Rest 1
    RETURN mod_result = 1;
END;
$$ LANGUAGE plpgsql;
</pre>
