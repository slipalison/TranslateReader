namespace TranslateReader.Contracts.Access;

/// <summary>
/// Thin, pass-through boundary over the native llama.cpp C API used by the iOS translation engine
/// (D-2026-08-16-llm-mobile-5). Every operation here maps 1:1 to a single native call: the
/// platform implementation (<c>src/TranslateReader/Platforms/iOS/LlamaNativeAccess.cs</c>) is pure
/// platform-interop declarations with zero control flow. The tokenize -> decode -> sample ->
/// detokenize generation loop -- the actual logic -- lives in
/// <c>Business.Engines.LlamaCppTranslationEngine</c>, the only thing that sequences these
/// primitives. Native handles (model, context) are private state of the implementation and never
/// appear in this contract: no raw pointer-sized types, matching the rule that already keeps SQL
/// out of <c>Contracts/Access</c>.
/// </summary>
public interface ILlamaNativeAccess
{
    /// <summary>Loads model weights from <paramref name="modelPath"/> into native memory.</summary>
    void LoadModel(string modelPath);

    /// <summary>Creates an inference context of <paramref name="contextSize"/> tokens for the
    /// currently loaded model.</summary>
    void CreateContext(int contextSize);

    /// <summary>Tokenizes <paramref name="text"/>, returning the resulting token ids in order.</summary>
    int[] Tokenize(string text);

    /// <summary>Runs a forward pass over <paramref name="tokens"/>, advancing the context state.</summary>
    void Decode(int[] tokens);

    /// <summary>Samples the next token from the current context state at the given
    /// <paramref name="temperature"/>.</summary>
    int SampleNextToken(float temperature);

    /// <summary>Returns the text for a single <paramref name="token"/>.</summary>
    string TokenToText(int token);

    /// <summary>Whether <paramref name="token"/> signals the end of generation.</summary>
    bool IsEndOfGeneration(int token);

    /// <summary>Clears the context's key/value cache so the next <see cref="Decode"/> call starts
    /// a fresh generation without reloading the model.</summary>
    void ResetContext();

    /// <summary>Releases the current inference context.</summary>
    void FreeContext();

    /// <summary>Releases the currently loaded model.</summary>
    void FreeModel();
}
