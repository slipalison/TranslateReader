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
            "para explicar tudo direitinho a todos os presentes que estavam ali reunidos naquela " +
            "sala. No final, desculpe, mas era tarde demais para qualquer mudança.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Alguma frase original razoavelmente longa o suficiente para este teste especifico.",
            legit, "English", "Brazilian Portuguese (PT-BR)");

        Assert.True(result);
    }

    // B-4: these SAME opening phrases are extremely common in fiction dialogue - the app's own
    // domain (EPUB prose) - and must never be flagged just because they appear, only when they
    // co-occur with meta-vocabulary about the act of translating (see ClassicRefusal above, which
    // still IS flagged for exactly that reason). Both translation directions the reviewer's fixtures
    // covered, verbatim.
    [Theory]
    [InlineData(
        "Desculpe, eu não quis te magoar com essas palavras tão duras naquela noite fria de inverno.",
        "English", "Brazilian Portuguese (PT-BR)")]
    [InlineData(
        "Não posso acreditar que isso está acontecendo bem diante dos meus próprios olhos incrédulos.",
        "English", "Brazilian Portuguese (PT-BR)")]
    [InlineData(
        "\"I can't breathe,\" she whispered as the walls of the small room seemed to close in around her.",
        "Brazilian Portuguese (PT-BR)", "English")]
    [InlineData(
        "\"I'm sorry for your loss,\" he said quietly, taking a seat beside her at the wooden table.",
        "Brazilian Portuguese (PT-BR)", "English")]
    public void IsPlausibleSnippetTranslation_FictionDialogueOpeningWithARefusalPhraseButNoMetaVocabulary_IsAccepted(
        string dialogue, string sourceLanguage, string targetLanguage)
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Alguma frase original qualquer.", dialogue, sourceLanguage, targetLanguage);

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
        // The excerpt is not always available to a caller of this overload - an arbitrarily long,
        // legitimate-looking response must not be rejected just because the length ratio has
        // nothing to compare against.
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            null, "Ela concordou rapidamente com a proposta apresentada durante a reunião.",
            "English", "Brazilian Portuguese (PT-BR)");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausiblePersistedSnippetTranslation_ClassicRefusalTextFromTheModel_IsRejected()
    {
        var result = SnippetValidationUtility.IsPlausiblePersistedSnippetTranslation(ClassicRefusal);

        Assert.False(result);
    }

    // B-4: the exact false positive the reviewer proved mechanically - a legitimate row is judged
    // purely by the language-agnostic blocklist at load time, never by the stopword ratio, so it
    // survives regardless of what the CURRENT app settings' language pair happens to be.
    [Theory]
    [InlineData("Desculpe, eu não quis te magoar com essas palavras tão duras naquela noite fria de inverno.")]
    [InlineData("Não posso acreditar que isso está acontecendo bem diante dos meus próprios olhos incrédulos.")]
    [InlineData("\"I can't breathe,\" she whispered as the walls of the small room seemed to close in around her.")]
    [InlineData("\"I'm sorry for your loss,\" he said quietly, taking a seat beside her at the wooden table.")]
    public void IsPlausiblePersistedSnippetTranslation_FictionDialogue_IsAccepted(string dialogue)
    {
        var result = SnippetValidationUtility.IsPlausiblePersistedSnippetTranslation(dialogue);

        Assert.True(result);
    }

    [Fact]
    public void IsPlausiblePersistedSnippetTranslation_NeverAppliesTheStopwordRatio()
    {
        // A row this short, or in a language with no stopword table at all, would still pass the
        // ratio check anyway - the point proven here is structural: an implausibly foreign-looking
        // but non-refusing long response is NEVER rejected at load time, because the ratio check is
        // never called at all (only ContainsRefusalOpening is).
        const string wrongLanguageNoRefusal =
            "The committee reviewed the proposal and decided to postpone the final vote until next week.";

        var result = SnippetValidationUtility.IsPlausiblePersistedSnippetTranslation(wrongLanguageNoRefusal);

        Assert.True(result);
    }
}
