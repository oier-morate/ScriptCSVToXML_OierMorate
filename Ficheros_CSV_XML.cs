using System;
using System.IO;
using System.Diagnostics;
using System.Xml;

class Ficheros_CSV_XML
{
    static string rutaCsv = "NetflixOriginals.csv";
    static string rutaXml = "NetflixOriginals.xml";

    static string[] titulos = new string[1000];
    static string[] generos = new string[1000];
    static string[] fechas = new string[1000];
    static string[] duraciones = new string[1000];
    static string[] puntuaciones = new string[1000];
    static string[] idiomas = new string[1000];
    static int total = 0;

    static string filtroTexto = "";
    static int filtroAnio = 0;

    // Para los logs de Windows
    static string fuenteLog = "ScriptCSVToXML";
    static bool logActivo = false;

    static void Main(string[] args)
    {
        PrepararLog();

        int opcion;
        do
        {
            Console.WriteLine("\n===== CSV <-> XML =====");
            Console.WriteLine("1. De CSV a XML");
            Console.WriteLine("2. De XML a CSV");
            Console.WriteLine("3. Salir");
            Console.Write("Elige una opción: ");
            int.TryParse(Console.ReadLine(), out opcion);

            if (opcion == 1)
            {
                SubMenu(true);
            }
            else if (opcion == 2)
            {
                Console.WriteLine("\nAVISO: el CSV de destino puede sobrescribir el original.");
                Console.WriteLine("Para evitarlo, cambia antes la ruta del CSV (opción 5), por ejemplo a NuevoCSV.csv.");
                SubMenu(false);
            }
            else if (opcion != 3)
            {
                Console.WriteLine("Opción no válida.");
            }
        } while (opcion != 3);
    }

    static void SubMenu(bool csvAXml)
    {
        int opcion;
        do
        {
            Console.WriteLine("\n===== MENÚ =====");
            Console.WriteLine("Ruta CSV: " + rutaCsv);
            Console.WriteLine("Ruta XML: " + rutaXml);
            Console.WriteLine("1. Exportar todo el documento");
            Console.WriteLine("2. Exportar por género");
            Console.WriteLine("3. Exportar por año");
            Console.WriteLine("4. Exportar por lenguaje");
            Console.WriteLine("5. Elegir la ruta del CSV");
            Console.WriteLine("6. Elegir la ruta del XML");
            Console.WriteLine("7. Volver");
            Console.Write("Elige una opción: ");
            int.TryParse(Console.ReadLine(), out opcion);

            if (opcion >= 1 && opcion <= 4)
            {
                if (csvAXml)
                {
                    CsvAXml(opcion);
                }
                else
                {
                    XmlACsv(opcion);
                }
            }
            else if (opcion == 5)
            {
                Console.Write("Ruta del CSV: ");
                rutaCsv = Console.ReadLine().Trim();
            }
            else if (opcion == 6)
            {
                Console.Write("Ruta del XML: ");
                rutaXml = Console.ReadLine().Trim();
            }
            else if (opcion != 7)
            {
                Console.WriteLine("Opción no válida.");
            }
        } while (opcion != 7);
    }

    //DE CSV A XML
    static void CsvAXml(int opcion)
    {
        try
        {
            Log("INICIO de la exportación de CSV a XML. Fichero origen (CSV): " + Path.GetFullPath(rutaCsv)
                + ". Fichero destino (XML): " + Path.GetFullPath(rutaXml));
            LeerCsv();
            if (!PedirFiltro(opcion))
            {
                Console.WriteLine("Dato no válido.");
                Log("Exportación de CSV a XML cancelada: dato del filtro no válido.");
                return;
            }
            EscribirXml(opcion);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    //DE XML A CSV
    static void XmlACsv(int opcion)
    {
        try
        {
            Log("INICIO de la exportación de XML a CSV. Fichero origen (XML): " + Path.GetFullPath(rutaXml)
                + ". Fichero destino (CSV): " + Path.GetFullPath(rutaCsv));
            LeerXml();
            if (!PedirFiltro(opcion))
            {
                Console.WriteLine("Dato no válido.");
                Log("Exportación de XML a CSV cancelada: dato del filtro no válido.");
                return;
            }

            // Para no sobreescribir el CSV original
            if (File.Exists(rutaCsv))
            {
                Console.Write("El fichero " + rutaCsv + " ya existe. ¿Sobrescribir? (s/n): ");
                if (Console.ReadLine().Trim().ToLower() != "s")
                {
                    Console.WriteLine("Cancelado. Cambia la ruta del CSV con la opción 5.");
                    Log("Exportación de XML a CSV cancelada: el usuario no quiere sobrescribir " + Path.GetFullPath(rutaCsv));
                    return;
                }
            }
            EscribirCsv(opcion);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    //LEER
    static void LeerCsv()
    {
        string[] lineas = File.ReadAllLines(rutaCsv);
        total = 0;

        // 1 para saltar cabecera
        for (int i = 1; i < lineas.Length; i++)
        {
            if (lineas[i].Trim() != "")
            {
                string[] campos = DividirLinea(lineas[i]);
                if (campos.Length == 6 && total < 1000)
                {
                    titulos[total] = campos[0];
                    generos[total] = campos[1];
                    fechas[total] = campos[2];
                    duraciones[total] = campos[3];
                    puntuaciones[total] = campos[4];
                    idiomas[total] = campos[5];
                    Log("Elemento leído del CSV: " + DatosPelicula(total));
                    total++;
                }
            }
        }
    }

    // Divide la línea por comas sin separar lo que va entre comillas
    // (por ejemplo "August 5, 2019")
    static string[] DividirLinea(string linea)
    {
        string limpia = "";
        bool enComillas = false;

        foreach (char c in linea)
        {
            if (c == '"')
            {
                enComillas = !enComillas;
            }
            else if (c == ',' && enComillas)
            {
                limpia += '|';
            }
            else
            {
                limpia += c;
            }
        }

        string[] campos = limpia.Split(',');
        for (int i = 0; i < campos.Length; i++)
        {
            campos[i] = campos[i].Replace('|', ','); // se devuelve la coma al de dentro de las comillas
        }
        return campos;
    }

    static void LeerXml()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(rutaXml);
        XmlNodeList nodos = doc.GetElementsByTagName("Pelicula");
        total = 0;

        foreach (XmlNode nodo in nodos)
        {
            if (total < 1000)
            {
                titulos[total] = nodo.SelectSingleNode("Title").InnerText;
                generos[total] = nodo.SelectSingleNode("Genre").InnerText;
                fechas[total] = nodo.SelectSingleNode("Premiere").InnerText;
                duraciones[total] = nodo.SelectSingleNode("Runtime").InnerText;
                puntuaciones[total] = nodo.SelectSingleNode("IMDBScore").InnerText;
                idiomas[total] = nodo.SelectSingleNode("Language").InnerText;
                Log("Elemento leído del XML: " + DatosPelicula(total));
                total++;
            }
        }
    }

    //FILTRO
    static bool PedirFiltro(int opcion)
    {
        if (opcion == 2)
        {
            filtroTexto = ElegirValor(generos, "género");
            return filtroTexto != null;
        }
        if (opcion == 4)
        {
            filtroTexto = ElegirValor(idiomas, "lenguaje");
            return filtroTexto != null;
        }
        if (opcion == 3)
        {
            Console.Write("Año: ");
            return int.TryParse(Console.ReadLine(), out filtroAnio);
        }  
        return true;
    }

    //Listar los generos sin repetir
    static string ElegirValor(string[] datos, string nombre)
    {
        string[] valores = new string[total];
        int cantidad = 0;

        for (int i = 0; i < total; i++)
        {
            bool repetido = false;
            for (int j = 0; j < cantidad; j++)
            {
                if (valores[j] == datos[i])
                {
                    repetido = true;
                }
            }
            if (!repetido)
            {
                valores[cantidad] = datos[i];
                cantidad++;
            }
        }

        Console.WriteLine("Opciones de " + nombre + ":");
        for (int i = 0; i < cantidad; i++)
        {
            Console.WriteLine((i + 1) + ". " + valores[i]);
        }

        Console.Write("Elige el número: ");
        int eleccion;
        if (int.TryParse(Console.ReadLine(), out eleccion) && eleccion >= 1 && eleccion <= cantidad)
        {
            return valores[eleccion - 1];
        }
        return null;
    }

    // Solo guarda las que cumple el filtro 
    static bool Cumple(int i, int opcion)
    {
        if (opcion == 2)
        {
            return generos[i] == filtroTexto;
        }
        if (opcion == 4)
        {
            return idiomas[i] == filtroTexto;
        }
        if (opcion == 3)
        {
            // El año son los 4 últimos caracteres de la fecha
            string fecha = fechas[i].Trim();
            int anio = int.Parse(fecha.Substring(fecha.Length - 4));
            return anio >= filtroAnio;
        }
        return true;
    }

    //ESCRIBIR
    static void EscribirXml(int opcion)
    {
        int exportadas = 0;
        using (StreamWriter sw = new StreamWriter(rutaXml))
        {
            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sw.WriteLine("<Peliculas>");
            for (int i = 0; i < total; i++)
            {
                if (Cumple(i, opcion))
                {
                    sw.WriteLine("  <Pelicula>");
                    sw.WriteLine("    <Title>" + ParaXml(titulos[i]) + "</Title>");
                    sw.WriteLine("    <Genre>" + ParaXml(generos[i]) + "</Genre>");
                    sw.WriteLine("    <Premiere>" + ParaXml(fechas[i]) + "</Premiere>");
                    sw.WriteLine("    <Runtime>" + ParaXml(duraciones[i]) + "</Runtime>");
                    sw.WriteLine("    <IMDBScore>" + ParaXml(puntuaciones[i]) + "</IMDBScore>");
                    sw.WriteLine("    <Language>" + ParaXml(idiomas[i]) + "</Language>");
                    sw.WriteLine("  </Pelicula>");
                    exportadas++;
                }
            }
            sw.WriteLine("</Peliculas>");
        }
        Console.WriteLine(exportadas + " película(s) exportada(s) a " + rutaXml);
        Log("FIN de la exportación de CSV a XML. Películas exportadas: " + exportadas
            + ". Fichero nuevo creado en: " + Path.GetFullPath(rutaXml));
    }

    static void EscribirCsv(int opcion)
    {
        int exportadas = 0;
        using (StreamWriter sw = new StreamWriter(rutaCsv))
        {
            sw.WriteLine("Title,Genre,Premiere,Runtime,IMDB Score,Language");
            for (int i = 0; i < total; i++)
            {
                if (Cumple(i, opcion))
                {
                    sw.WriteLine(ParaCsv(titulos[i]) + "," + ParaCsv(generos[i]) + ","
                        + ParaCsv(fechas[i]) + "," + ParaCsv(duraciones[i]) + ","
                        + ParaCsv(puntuaciones[i]) + "," + ParaCsv(idiomas[i]));
                    exportadas++;
                }
            }
        }
        Console.WriteLine(exportadas + " película(s) exportada(s) a " + rutaCsv);
        Log("FIN de la exportación de XML a CSV. Películas exportadas: " + exportadas
            + ". Fichero nuevo creado en: " + Path.GetFullPath(rutaCsv));
    }

    // En XML los símbolos & < > no se pueden escribir tal cual
    static string ParaXml(string texto)
    {
        return texto.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    // En CSV, si un campo lleva coma, se pone entre comillas
    static string ParaCsv(string campo)
    {
        if (campo.Contains(","))
        {
            return "\"" + campo + "\"";
        }
        return campo;
    }

    //LOGS DE WINDOWS
    // Prepara el log. Hay que ejecutar el programa como administrador
    static void PrepararLog()
    {
        try
        {
            if (!EventLog.SourceExists(fuenteLog))
            {
                EventLog.CreateEventSource(fuenteLog, "Application");
                Console.WriteLine("Fuente de log creada. Cierra el programa y ejecútalo otra vez para empezar a guardar logs.");
            }
            else
            {
                logActivo = true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("No se pueden usar los logs de Windows (ejecuta como administrador): " + e.Message);
        }
    }

    // Escribe una línea en el log de Windows (Visor de eventos)
    static void Log(string mensaje)
    {
        if (logActivo)
        {
            try
            {
                EventLog.WriteEntry(fuenteLog, mensaje, EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                logActivo = false;
                Console.WriteLine("Se desactivan los logs: " + e.Message);
            }
        }
    }

    // Devuelve los datos de la película número i en un texto
    static string DatosPelicula(int i)
    {
        return "Title = " + titulos[i] + ", Genre = " + generos[i] + ", Premiere = " + fechas[i]
            + ", Runtime = " + duraciones[i] + ", IMDBScore = " + puntuaciones[i] + ", Language = " + idiomas[i];
    }
}