namespace LimousineBooking.Application.Reports;

/// <summary>
/// Localized column headers for the admin CSV exports (Prompt 16, section 44) —
/// only the headers are translated, never the data values themselves (a booking
/// reference, "Basel", "CHF" all stay exactly as stored). An unsupported/missing
/// language code falls back to English, same as everywhere else in the app.
/// </summary>
public static class ReportCsvLabels
{
    public static string[] Bookings(string languageCode) => languageCode switch
    {
        "de" => new[] { "Buchungsnummer", "Datum", "Zeit", "Route", "Kunde", "Fahrer", "Fahrzeug", "Passagiere", "Preis", "Währung", "Status", "Fahrtstatus" },
        "fr" => new[] { "Référence de réservation", "Date", "Heure", "Trajet", "Client", "Chauffeur", "Véhicule", "Passagers", "Prix", "Devise", "Statut", "Statut du trajet" },
        _ => new[] { "Booking Reference", "Date", "Time", "Route", "Customer", "Driver", "Vehicle", "Passengers", "Price", "Currency", "Status", "Ride Status" }
    };

    public static string[] Routes(string languageCode) => languageCode switch
    {
        "de" => new[] { "Abfahrt", "Ziel", "Anzahl Buchungen", "Umsatz", "Anteil an Gesamtbuchungen" },
        "fr" => new[] { "Départ", "Destination", "Nombre de réservations", "Revenu", "Pourcentage du total des réservations" },
        _ => new[] { "Departure", "Destination", "Booking Count", "Revenue", "Percentage Of Total Bookings" }
    };

    public static string[] Drivers(string languageCode) => languageCode switch
    {
        "de" => new[] { "Fahrer", "Zugewiesen", "Abgeschlossen", "Storniert", "Bevorstehend", "Manuelle Zuweisungen", "Abschlussquote" },
        "fr" => new[] { "Chauffeur", "Attribuées", "Terminées", "Annulées", "À venir", "Attributions manuelles", "Taux d'achèvement" },
        _ => new[] { "Driver", "Assigned", "Completed", "Cancelled", "Upcoming", "Manual Assignments", "Completion Rate" }
    };

    public static string[] Vehicles(string languageCode) => languageCode switch
    {
        "de" => new[] { "Fahrzeug", "Zugewiesen", "Abgeschlossen", "Bevorstehend", "Passagiere gesamt", "Auslastung (Buchungen)" },
        "fr" => new[] { "Véhicule", "Attribuées", "Terminées", "À venir", "Total des passagers", "Utilisation (réservations)" },
        _ => new[] { "Vehicle", "Assigned", "Completed", "Upcoming", "Total Passengers", "Utilization (Booking Count)" }
    };
}
