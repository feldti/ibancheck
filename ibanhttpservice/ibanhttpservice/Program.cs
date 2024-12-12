using System;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text;
using System.Web;
using System.Threading.Tasks;

class Program
{
    // Beispiel einer internen Funktion, die die Eingabe verarbeitet


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
    
    private static string ProcessInput(string input)
    {
        string testIban = input;
        string csvFilePath = "blz-aktuell-csv-data.csv";

        if (ValidateIban(testIban, out string blz))
        {
            var (bankName, plz, ort) = GetBankDetails(blz, csvFilePath);
            if (String.IsNullOrEmpty(bankName))
            {
                // Formal gültige IBAN; Bank nicht gefunden: true;2;IBAN;BLZ
                return ($"true;2;{testIban};{blz}");
            }
            else
            {
                if (bankName == "no")
                {
                    // Formal gültige IBAN; Bankdatei nicht gefunden: true;3;IBAN;BLZ
                    return ($"true;3;{testIban};{blz}");
                }
                else
                {
                    // Formal gültige IBAN; Bankdatei gefunden: true;0;IBAN;BLZ;PLZ;ORT;BANKNAME
                    return ($"true;0;{testIban};{blz};{plz};{ort},{bankName}");
                }
                
            }
            
        }
        else
        {
            // Formal ungültige IBAN; Bankdatei gefunden: false;1;IBAN
            return ($"false;1;{testIban}");
        }
    }
    
    static async Task Main(string[] args)
    {
        // Port definieren (Standard: 5000, kann als Argument übergeben werden)
        int port = args.Length > 0 ? int.Parse(args[0]) : 5000;

        // HTTP-Listener erstellen
        HttpListener listener = new HttpListener();
        string prefix = $"http://*:{port}/";
        listener.Prefixes.Add(prefix);

        // Listener starten
        listener.Start();
        Console.WriteLine($"HTTP Server läuft auf {prefix}. Drücken Sie STRG+C zum Beenden.");

        while (true)
        {
            try
            {
                // Auf eingehende Anfragen warten
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                // Prüfen, ob es eine GET-Anfrage ist
                if (request.HttpMethod != "GET")
                {
                    response.StatusCode = 405; // Method Not Allowed
                    response.Close();
                    continue;
                }

                // Abfrageparameter auslesen
                string input = HttpUtility.ParseQueryString(request.Url.Query).Get("iban");

                if (string.IsNullOrEmpty(input))
                {
                    // Fehler, wenn kein Parameter übergeben wurde
                    response.StatusCode = 400; // Bad Request
                    byte[] errorBuffer = Encoding.UTF8.GetBytes("Bitte einen 'input'-Parameter übergeben.");
                    response.ContentType = "text/plain";
                    response.ContentLength64 = errorBuffer.Length;
                    await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                    response.Close();
                    continue;
                }

                // Eingabe an interne Funktion übergeben
                string result = ProcessInput(input);

                // Ergebnis zurückgeben
                byte[] buffer = Encoding.UTF8.GetBytes(result);
                response.ContentType = "text/plain";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                // Verbindung schließen
                response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler: {ex.Message}");
            }
        }
    }
}
