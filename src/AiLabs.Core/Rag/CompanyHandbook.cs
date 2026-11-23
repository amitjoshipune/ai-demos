namespace AiLabs.Rag;

/// <summary>Tiny private corpus so RAG, tools, and MCP all share one source of truth.</summary>
public static class CompanyHandbook
{
    public const string Text = """
        # Contoso employee handbook (practice data)

        ## Annual leave
        Full-time employees get 20 days of annual leave per calendar year.
        Leave requests need a manager's approval at least 5 working days in advance.
        Unused leave does not carry over after 31 December.

        ## Laptop refresh
        Laptops are refreshed every 4 years.
        Lost or damaged devices must be reported to IT within 24 hours.
        Personal software installs need IT approval.

        ## Expenses
        Travel meals are capped at 40 USD per day.
        Receipts are required for any expense over 15 USD.
        Submit claims within 14 days of the trip.
        """;
}
