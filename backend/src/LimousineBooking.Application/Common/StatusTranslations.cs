using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Application.Common;

/// <summary>
/// Localizes the technical enum values (BookingStatus/RideStatus/PaymentStatus)
/// into short, natural display text for the four supported languages — used only
/// at presentation edges (rendered emails today; the frontend does its own
/// equivalent translation via i18next). The API itself and the domain model never
/// see or store these strings (section 57/58/59) — <c>BookingResponse.Status</c>
/// etc. always stay the raw enum name, e.g. "Confirmed", never a translated word.
/// Non-ASCII characters are written as \u escapes rather than literal source
/// characters, to guarantee the exact intended codepoint regardless of any
/// tooling/editor encoding along the way.
/// </summary>
public static class StatusTranslations
{
    public static string Translate(BookingStatus status, string languageCode) =>
        Lookup(BookingStatusLabels, status.ToString(), languageCode);

    public static string Translate(RideStatus status, string languageCode) =>
        Lookup(RideStatusLabels, status.ToString(), languageCode);

    public static string Translate(PaymentStatus status, string languageCode) =>
        Lookup(PaymentStatusLabels, status.ToString(), languageCode);

    private static string Lookup(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> table, string key, string languageCode)
    {
        if (!table.TryGetValue(key, out var byLanguage))
            return key;

        var normalized = Domain.Common.SupportedLanguages.Normalize(languageCode);
        return byLanguage.TryGetValue(normalized, out var label) ? label : byLanguage[Domain.Common.SupportedLanguages.Default];
    }

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> BookingStatusLabels =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["Pending"] = Labels("Pending", "Ausstehend", "En attente", "In attesa"),
            ["Confirmed"] = Labels("Confirmed", "Bestätigt", "Confirmée", "Confermata"),
            ["PendingManualAssignment"] = Labels("Pending (manual assignment)", "Ausstehend (manuelle Zuweisung)", "En attente (attribution manuelle)", "In attesa (assegnazione manuale)"),
            ["Cancelled"] = Labels("Cancelled", "Storniert", "Annulée", "Annullata"),
            ["Assigned"] = Labels("Assigned", "Zugewiesen", "Attribuée", "Assegnata"),
            ["OnTheWay"] = Labels("On the way", "Unterwegs", "En route", "In arrivo"),
            ["PassengerPickedUp"] = Labels("Passenger picked up", "Fahrgast abgeholt", "Passager pris en charge", "Passeggero prelevato"),
            ["Completed"] = Labels("Completed", "Abgeschlossen", "Terminée", "Completata")
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> RideStatusLabels =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["Upcoming"] = Labels("Upcoming", "Bevorstehend", "À venir", "In programma"),
            ["OnTheWay"] = Labels("On the way", "Unterwegs", "En route", "In arrivo"),
            ["PassengerPickedUp"] = Labels("Passenger picked up", "Fahrgast abgeholt", "Passager pris en charge", "Passeggero prelevato"),
            ["Completed"] = Labels("Completed", "Abgeschlossen", "Terminée", "Completata"),
            ["Cancelled"] = Labels("Cancelled", "Storniert", "Annulée", "Annullata")
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PaymentStatusLabels =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["Pending"] = Labels("Pending", "Ausstehend", "En attente", "In attesa"),
            ["Processing"] = Labels("Processing", "In Bearbeitung", "En cours de traitement", "In elaborazione"),
            ["Paid"] = Labels("Paid", "Bezahlt", "Payée", "Pagato"),
            ["Failed"] = Labels("Failed", "Fehlgeschlagen", "Échouée", "Non riuscito"),
            ["Cancelled"] = Labels("Cancelled", "Storniert", "Annulée", "Annullato"),
            ["Refunded"] = Labels("Refunded", "Erstattet", "Remboursée", "Rimborsato")
        };

    private static IReadOnlyDictionary<string, string> Labels(string en, string de, string fr, string it) =>
        new Dictionary<string, string> { ["en"] = en, ["de"] = de, ["fr"] = fr, ["it"] = it };
}
