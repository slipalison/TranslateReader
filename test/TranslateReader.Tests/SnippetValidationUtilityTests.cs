using TranslateReader.Utilities;

namespace TranslateReader.Tests;

public class SnippetValidationUtilityTests
{
    // Verbatim text observed in production (a small local model's false-positive safety refusal on
    // innocuous business text) - model output, never real user data, kept as a fixture so the exact
    // defect that reached the reader can never regress silently.
    private const string ClassicRefusal =
        "No, I cannot provide a translation of this text. It contains explicit sexual content, " +
        "which violates my safety guidelines.";

    [Fact]
    public void IsPlausibleSnippetTranslation_ClassicRefusalTextFromTheModel_IsRejected()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Texto original qualquer.", ClassicRefusal, "English", "Brazilian Portuguese (PT-BR)");

        Assert.False(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_LegitimateEnglishResponseWhenTargetIsEnglish_IsAccepted()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "El comité revisó la propuesta.",
            "The committee members agreed to review the proposal again next month.",
            "Spanish", "English");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_LegitimatePortugueseResponse_IsAcceptedByRatio()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "She quickly agreed with the proposal presented during the meeting.",
            "Ela concordou rapidamente com a proposta apresentada durante a reunião.",
            "English", "Brazilian Portuguese (PT-BR)");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_ShortTranslatedTextSkipsLanguageValidation()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Original text.", "Xyzzy plugh.", "English", "Brazilian Portuguese (PT-BR)");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_TargetLanguageWithoutStopwordTable_SkipsLanguageValidation()
    {
        var noTableResponse = "Ceci est une phrase suffisamment longue pour dépasser le seuil de quarante caractères.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Original text long enough to matter here.", noTableResponse, "English", "French");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_SourceEqualsTarget_SkipsLanguageValidation()
    {
        var response = "The committee members agreed to review the proposal again next month.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Some original.", response, "English", "English");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_BlocklistCatchesARefusalAtTheStart()
    {
        const string refusal =
            "I'm sorry, but I cannot help with that request due to the nature of the content.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Original.", refusal, "English", "Brazilian Portuguese (PT-BR)");

        Assert.False(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_BlocklistDoesNotFlagTheSamePhraseInTheMiddleOfALegitTranslation()
    {
        const string legit =
            "Ela disse que não podia ajudar com isso agora, mas prometeu que voltaria mais tarde " +
            "para explicar tudo direitinho a todos os presentes na sala. No final, desculpe, mas " +
            "era tarde demais.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Alguma frase original longa o suficiente.", legit, "English", "Brazilian Portuguese (PT-BR)");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_ImplausiblyLongResponse_IsRejectedWhenOriginalTextIsKnown()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Ela disse que sim.", new string('x', 500), "English", "Brazilian Portuguese (PT-BR)");

        Assert.False(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_OriginalTextNull_SkipsTheLengthRatioCheck()
    {
        // At load-time purge the original excerpt was never persisted (only its hash) - an
        // arbitrarily long legitimate-looking response must not be rejected just for that reason.
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            null, "Ela concordou rapidamente com a proposta apresentada durante a reunião.",
            "English", "Brazilian Portuguese (PT-BR)");

        Assert.True(result);
    }
}
