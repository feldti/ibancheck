using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

class IBANValidator
{
    // Liste der IBAN-Längen nach Ländern
    private static readonly Dictionary<string, int> IbanLengths = new Dictionary<string, int>
    {
        { "DE", 22 }, // Deutschland
        { "AT", 20 }, // Österreich
        { "CH", 21 }, // Schweiz
        { "FR", 27 }, // Frankreich
        { "ES", 24 }, // Spanien
        { "IT", 27 }, // Italien
        { "NL", 18 }, // Niederlande
        { "BE", 16 }, // Belgien
        { "PL", 28 }  // Polen
        // Weitere Länder können hinzugefügt werden
    };

    public static bool ValidateIban(string iban, out string blz)
    {
        blz = string.Empty;

        if (string.IsNullOrWhiteSpace(iban))
            return false;

        iban = iban.Replace(" ", string.Empty).ToUpper(); // Entfernen von Leerzeichen und Großschreibung
        if (iban.Length < 4 || !iban.All(char.IsLetterOrDigit))
            return false;

        // Extrahieren der Länderkennung und Prüfziffer
        string countryCode = iban.Substring(0, 2);
        string checkDigits = iban.Substring(2, 2);

        // Verschieben der ersten vier Zeichen ans Ende
        string rearranged = iban.Substring(4) + countryCode + checkDigits;

        // Buchstaben in Zahlen umwandeln (A=10, B=11, ..., Z=35)
        string numericIban = string.Concat(rearranged.Select(c =>
            char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));

        // Prüfen der Prüfziffer mit Modulo 97
        bool isValid = BigInteger.Parse(numericIban) % 97 == 1;
        // Console.WriteLine(numericIban);
        // Console.WriteLine(BigInteger.Parse(numericIban) % 97);
        if (isValid && countryCode == "DE")
        {
            // Extrahieren der Bankleitzahl (Stellen 5-12 für deutsche IBANs)
            blz = iban.Substring(4, 8);
        }

        return isValid;
    }
    
    public static (string BankName, string Plz, string ort) GetBankDetails(string blz, string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
            return ("no", "", "");

        // ISO-8859-1 Encoding verwenden
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        using (var reader = new StreamReader(csvFilePath, encoding))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var columns = line.Split(';');
                if (columns.Length > 4 && columns[0].Trim('"') == blz)
                {
                    string bankName = columns[2].Trim('"'); // Bankname
                    string plz = columns[3].Trim('"');      // PLZ
                    string ort = columns[4].Trim('"');      // Ort
                    return (bankName, plz, ort);
                }
            }
        }

        return ("", "", "");
    }

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Bitte geben Sie eine IBAN als Parameter an.");
            return;
        }

        string testIban = args[0];
        string csvFilePath = "blz-aktuell-csv-data.csv";

        if (ValidateIban(testIban, out string blz))
        {
            var (bankName, plz, ort) = GetBankDetails(blz, csvFilePath);
            if (String.IsNullOrEmpty(bankName))
            {
                // Formal gültige IBAN; Bank nicht gefunden: true;2;IBAN;BLZ
                Console.WriteLine($"true;2;{testIban};{blz}");
            }
            else
            {
                if (bankName == "no")
                {
                    // Formal gültige IBAN; Bankdatei nicht gefunden: true;3;IBAN;BLZ
                    Console.WriteLine($"true;3;{testIban};{blz}");
                }
                else
                {
                    // Formal gültige IBAN; Bankdatei gefunden: true;0;IBAN;BLZ;PLZ;ORT;BANKNAME
                    Console.WriteLine($"true;0;{testIban};{blz};{plz};{ort},{bankName}");
                }
                
            }
            
        }
        else
        {
            // Formal ungültige IBAN; Bankdatei gefunden: false;1;IBAN
            Console.WriteLine($"false;1;{testIban}");
        }
    }
}
