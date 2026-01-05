namespace PairAdmin.Security;

/// <summary>
/// Default sensitive data patterns
/// </summary>
public static class DefaultPatterns
{
    /// <summary>
    /// Gets all default patterns
    /// </summary>
    public static List<IFilterPattern> GetAllPatterns()
    {
        return new List<IFilterPattern>
        {
            // AWS Access Key ID
            new RegexPattern(
                "AWS Access Key",
                @"AKIA[0-9A-Z]{16}",
                preserveLength: 4),

            // AWS Secret Key (40 char base64)
            new RegexPattern(
                "AWS Secret Key",
                @"[0-9a-zA-Z/+]{40}",
                preserveLength: 4),

            // OpenAI API Key
            new RegexPattern(
                "OpenAI API Key",
                @"sk-[a-zA-Z0-9]{48}",
                preserveLength: 7),

            // GitHub Personal Access Token
            new RegexPattern(
                "GitHub Token",
                @"ghp_[a-zA-Z0-9]{36}",
                preserveLength: 4),

            // GitHub App Installation Token
            new RegexPattern(
                "GitHub App Token",
                @"ghu_[a-zA-Z0-9]{36}",
                preserveLength: 4),

            // GitHub Refresh Token
            new RegexPattern(
                "GitHub Refresh Token",
                @"ghr_[a-zA-Z0-9]{36}",
                preserveLength: 4),

            // Google API Key
            new RegexPattern(
                "Google API Key",
                @"AIza[0-9A-Za-z-_]{35}",
                preserveLength: 4),

            // Stripe API Key (live)
            new RegexPattern(
                "Stripe Live Key",
                @"sk_live_[0-9a-zA-Z]{24,}",
                preserveLength: 4),

            // Stripe Test Key
            new RegexPattern(
                "Stripe Test Key",
                @"sk_test_[0-9a-zA-Z]{24,}",
                preserveLength: 4),

            // Generic Private Key Header
            new RegexPattern(
                "Private Key",
                @"-----BEGIN (?:RSA |EC |DSA |OPENSSH |PGP )?PRIVATE KEY-----"),

            // SSH Private Key
            new RegexPattern(
                "SSH Private Key",
                @"-----BEGIN OPENSSH PRIVATE KEY-----"),

            // Password assignment patterns
            new RegexPattern(
                "Password Assignment",
                @"(?i)(password|passwd|pwd|secret|api_key|apikey|api-key)[\s]*[:=][\s]*[^\s]+",
                preserveLength: 0),

            // Environment variable with secret
            new RegexPattern(
                "Secret Env Var",
                @"(?i)(export[\s]+)?(PASSWORD|SECRET|API_KEY|TOKEN|AUTH)[\s]*=[\s]*[^\s]+",
                preserveLength: 0),

            // Credit Card Number (basic pattern)
            new RegexPattern(
                "Credit Card",
                @"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})\b",
                preserveLength: 4),

            // Email Address
            new RegexPattern(
                "Email Address",
                @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
                preserveLength: 0),

            // Social Security Number (US)
            new RegexPattern(
                "SSN",
                @"\b[0-9]{3}-[0-9]{2}-[0-9]{4}\b",
                preserveLength: 0),

            // JWT Token
            new RegexPattern(
                "JWT Token",
                @"eyJ[a-zA-Z0-9_-]*\.eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*",
                preserveLength: 10),

            // Bearer Token
            new RegexPattern(
                "Bearer Token",
                @"(?i)Bearer\s+[a-zA-Z0-9\-\._~\+\/]+=*",
                preserveLength: 7),

            // Basic Auth Header
            new RegexPattern(
                "Basic Auth",
                @"(?i)Basic\s+[a-zA-Z0-9+/]+=*",
                preserveLength: 6),

            // Database Connection String
            new RegexPattern(
                "Connection String",
                @"(?i)(server|host|user id|uid|password|pwd)=[^;]+",
                preserveLength: 0),

            // Slack Token
            new RegexPattern(
                "Slack Token",
                @"xox[baprs]-([0-9a-zA-Z]{10,48})",
                preserveLength: 4),

            // Twilio Account SID
            new RegexPattern(
                "Twilio SID",
                @"AC[a-z0-9]{32}",
                preserveLength: 2),

            // Twilio Auth Token
            new RegexPattern(
                "Twilio Auth",
                @"[a-z0-9]{32}",
                preserveLength: 4),

            // SendGrid API Key
            new RegexPattern(
                "SendGrid Key",
                @"SG\.[a-zA-Z0-9_-]{22}\.[a-zA-Z0-9_-]{43}",
                preserveLength: 3),

            // NPM Token
            new RegexPattern(
                "NPM Token",
                @"npm_[a-zA-Z0-9]{36}",
                preserveLength: 4),

            // Heroku API Key
            new RegexPattern(
                "Heroku Key",
                @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
                preserveLength: 8),

            // Azure Storage Key
            new RegexPattern(
                "Azure Storage Key",
                @"[A-Za-z0-9+\/]{86}==",
                preserveLength: 4),

            // IP Address (might indicate sensitive network info)
            new RegexPattern(
                "Private IP",
                @"\b(?:10\.\d{1,3}\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3}|172\.(?:1[6-9]|2\d|3[0-1])\.\d{1,3}\.\d{1,3})\b",
                preserveLength: 0),
        };
    }

    /// <summary>
    /// Gets high-priority patterns (most common sensitive data)
    /// </summary>
    public static List<IFilterPattern> GetHighPriorityPatterns()
    {
        return new List<IFilterPattern>
        {
            new RegexPattern("AWS Access Key", @"AKIA[0-9A-Z]{16}", preserveLength: 4),
            new RegexPattern("OpenAI API Key", @"sk-[a-zA-Z0-9]{48}", preserveLength: 7),
            new RegexPattern("GitHub Token", @"ghp_[a-zA-Z0-9]{36}", preserveLength: 4),
            new RegexPattern("Password Assignment", @"(?i)(password|passwd|pwd|secret)[\s]*[:=][\s]*[^\s]+", preserveLength: 0),
            new RegexPattern("Private Key", @"-----BEGIN (?:RSA |EC |DSA |OPENSSH |PGP )?PRIVATE KEY-----"),
            new RegexPattern("Bearer Token", @"(?i)Bearer\s+[a-zA-Z0-9\-\._~\+\/]+=*", preserveLength: 7),
        };
    }
}
