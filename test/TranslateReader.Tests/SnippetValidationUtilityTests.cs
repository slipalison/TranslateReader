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

    // B-5: the reviewer's fixture #3, kept BYTE FOR BYTE (never padded/lengthened) after the
    // reviewer mechanically proved a lengthened stand-in was used in a previous round to dodge this
    // exact ratio failure. If this literal string ever fails again, the code is wrong - not the
    // fixture.
    private const string ReviewerFixtureThree =
        "\"I can't breathe,\" she whispered, afraid of everything around her.";

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
        "\"I'm sorry for your loss,\" he said quietly, taking a seat beside her at the wooden table.",
        "Brazilian Portuguese (PT-BR)", "English")]
    public void IsPlausibleSnippetTranslation_FictionDialogueOpeningWithARefusalPhraseButNoMetaVocabulary_IsAccepted(
        string dialogue, string sourceLanguage, string targetLanguage)
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Alguma frase original qualquer.", dialogue, sourceLanguage, targetLanguage);

        Assert.True(result);
    }

    // B-5 (blocking, mechanically proven by the reviewer): this exact fixture was rejected in the
    // fresh path - not by the blocklist (it already passed there), but by the ratio, because the
    // English stopword table had no pronouns/auxiliaries and this dialogue leans entirely on them.
    [Fact]
    public void IsPlausibleSnippetTranslation_ReviewerFixtureThreeVerbatim_IsAcceptedInTheFreshPath()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Alguma frase original qualquer.", ReviewerFixtureThree, "Brazilian Portuguese (PT-BR)", "English");

        Assert.True(result);
    }

    // B-5: ordinary short EN narration with no dialogue punctuation at all, failing the SAME
    // enriched-table gap fixture #3 exposed - proves the fix is the table, not something specific to
    // quoted dialogue.
    [Fact]
    public void IsPlausibleSnippetTranslation_OrdinaryEnglishNarration_IsAcceptedByTheEnrichedRatio()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Alguma frase original qualquer.",
            "He nodded slowly and walked away without a word.",
            "Brazilian Portuguese (PT-BR)", "English");

        Assert.True(result);
    }

    // B-5: short PT-BR dialogue, target PT-BR - the reviewer's example of the same failure mode in
    // the other direction.
    [Fact]
    public void IsPlausibleSnippetTranslation_ShortPortugueseDialogue_IsAccepted()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Alguma frase original qualquer.",
            "— Não sei — disse ele, olhando para o chão.",
            "English", "Brazilian Portuguese (PT-BR)");

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
    public void IsPlausibleSnippetTranslation_OriginalTextNull_SkipsTheSentenceCountCheck()
    {
        var manySentences = "Um. Dois. Tres. Quatro. Cinco. Seis.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            null, manySentences, "English", "Brazilian Portuguese (PT-BR)");

        Assert.True(result);
    }

    // Iter 11 (A2/A3): the measured leak from the "Righting Software" book - a single 134-char
    // original sentence, translated as itself PLUS the next sentence from the context window
    // (D-B, iter 10). The original excerpt is quoted verbatim from the incident report; the exact
    // leaked continuation was not captured byte-for-byte in the report, so the translated text below
    // is a representative reconstruction with the SAME reported shape (~399 chars, 3 sentences: a
    // real translation of the original plus two more). Rejected by BOTH new layers independently -
    // see the two isolated tests right below, which pin each layer separately using shorter/longer
    // variants of this same shape so a regression in either one is caught even if the other still
    // happens to catch this particular case.
    private const string MeasuredLeakOriginal =
        "A customer will hardly ever present you with the requirements in a useful format, " +
        "let alone in a way that is conducive to good design.";

    [Fact]
    public void IsPlausibleSnippetTranslation_MeasuredLeak_IsRejected()
    {
        const string leaked =
            "Um cliente dificilmente lhe apresentará os requisitos em um formato útil, muito menos de " +
            "uma maneira propícia a um bom design. Você deve sempre transformar esses requisitos " +
            "recebidos do cliente em informações estruturadas antes de seguir adiante com o projeto. " +
            "No início do processo de design, um formato mais claro e organizado se torna muito mais " +
            "natural para todos os envolvidos na equipe.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            MeasuredLeakOriginal, leaked, "English", "Brazilian Portuguese (PT-BR)");

        Assert.False(result);
    }

    // Isolates the sentence-count layer: short enough to pass the length ratio on its own (122 chars
    // against a 341.2-char ceiling for this original), but still 3 sentences against an original of 1
    // (ceiling 2) - if sentence counting alone were disabled, this fixture would pass.
    [Fact]
    public void IsPlausibleSnippetTranslation_MeasuredLeakShape_SentenceCountAloneRejects()
    {
        const string threeShortSentences =
            "Um cliente raramente apresenta os requisitos. Isso raramente ocorre de forma útil. " +
            "Um bom design exige muito mais cuidado.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            MeasuredLeakOriginal, threeShortSentences, "English", "Brazilian Portuguese (PT-BR)");

        Assert.False(result);
    }

    // Isolates the length-ratio layer: only 2 sentences against an original of 1 (ceiling 2, passes
    // sentence counting on its own), but 363 chars against a 341.2-char ceiling - if the ratio alone
    // were disabled, this fixture would pass.
    [Fact]
    public void IsPlausibleSnippetTranslation_MeasuredLeakShape_LengthRatioAloneRejects()
    {
        const string twoVerboseSentences =
            "Um cliente dificilmente apresentará a você os requisitos originais em um formato " +
            "verdadeiramente útil e bem estruturado, muito menos de uma maneira que seja propícia a " +
            "um excelente processo de design de software orientado a serviços. Isso torna o trabalho " +
            "do arquiteto ainda mais difícil e demorado do que já costuma ser em projetos " +
            "convencionais de grande porte.";

        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            MeasuredLeakOriginal, twoVerboseSentences, "English", "Brazilian Portuguese (PT-BR)");

        Assert.False(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_SingleSentenceTranslation_IsAccepted()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Ela disse que sim.", "She said yes.", "Brazilian Portuguese (PT-BR)", "English");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_TranslationThatBreaksOnePeriodIntoTwo_IsAcceptedBySlack()
    {
        // The +1 sentence-count slack exists precisely for a translator that legitimately splits one
        // long original sentence into two shorter ones.
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "Ela disse que sim.", "She said yes. She meant it.", "Brazilian Portuguese (PT-BR)", "English");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_ThreeOriginalSentencesToFour_IsAcceptedBySlack()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "One. Two. Three.", "Um. Dois. Tres. Quatro.", "English", "Brazilian Portuguese (PT-BR)");

        Assert.True(result);
    }

    [Fact]
    public void IsPlausibleSnippetTranslation_ThreeOriginalSentencesToFive_IsRejected()
    {
        var result = SnippetValidationUtility.IsPlausibleSnippetTranslation(
            "One. Two. Three.", "Um. Dois. Tres. Quatro. Cinco.", "English", "Brazilian Portuguese (PT-BR)");

        Assert.False(result);
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
    [InlineData("\"I'm sorry for your loss,\" he said quietly, taking a seat beside her at the wooden table.")]
    public void IsPlausiblePersistedSnippetTranslation_FictionDialogue_IsAccepted(string dialogue)
    {
        var result = SnippetValidationUtility.IsPlausiblePersistedSnippetTranslation(dialogue);

        Assert.True(result);
    }

    // B-5: fixture #3, byte for byte - the reviewer already noted this one passes the persisted
    // (blocklist-only) path; pinned explicitly so it never silently regresses alongside the fresh-path fix.
    [Fact]
    public void IsPlausiblePersistedSnippetTranslation_ReviewerFixtureThreeVerbatim_IsAccepted()
    {
        var result = SnippetValidationUtility.IsPlausiblePersistedSnippetTranslation(ReviewerFixtureThree);

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
