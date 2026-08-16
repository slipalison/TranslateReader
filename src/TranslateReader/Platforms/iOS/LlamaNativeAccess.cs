using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TranslateReader.Contracts.Access;

namespace TranslateReader;

// WHY this file stays declarations-only: it is the ONLY thing in the app allowed to touch
// llama.cpp's C API directly (D-2026-08-16-llm-mobile-5). Every member below is a one-line
// pass-through to a single native call declared in this same file; nothing here branches,
// iterates, or handles an exception. The actual generation loop (tokenize -> decode -> sample ->
// detokenize, streaming, cancellation) is Business.Engines.LlamaCppTranslationEngine in
// TranslateReader.Core, which is unit-tested against this type's interface with NSubstitute. A
// change here that seems to need a loop or a branch belongs in that engine instead.
public sealed partial class LlamaNativeAccess : ILlamaNativeAccess
{
    private const int MaxTokenBufferSize = 4096;

    private nint _model;
    private nint _context;

    [LibraryImport("__Internal", EntryPoint = "tr_llama_load_model", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint NativeLoadModel(string modelPath);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_create_context")]
    private static partial nint NativeCreateContext(nint model, int contextSize);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_tokenize", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int NativeTokenize(
        nint context,
        string text,
        [MarshalUsing(CountElementName = "capacity")] int[] tokenBuffer,
        int capacity);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_decode")]
    private static partial void NativeDecode(
        nint context,
        [MarshalUsing(CountElementName = "count")] int[] tokens,
        int count);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_sample_next_token")]
    private static partial int NativeSampleNextToken(nint context, float temperature);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_token_to_text")]
    private static partial nint NativeTokenToText(nint context, int token);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_is_end_of_generation")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static partial bool NativeIsEndOfGeneration(nint context, int token);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_reset_context")]
    private static partial void NativeResetContext(nint context);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_free_context")]
    private static partial void NativeFreeContext(nint context);

    [LibraryImport("__Internal", EntryPoint = "tr_llama_free_model")]
    private static partial void NativeFreeModel(nint model);

    public void LoadModel(string modelPath) => _model = NativeLoadModel(modelPath);

    public void CreateContext(int contextSize) => _context = NativeCreateContext(_model, contextSize);

    public int[] Tokenize(string text)
    {
        var buffer = new int[MaxTokenBufferSize];
        var tokenCount = NativeTokenize(_context, text, buffer, buffer.Length);
        return buffer[..tokenCount];
    }

    public void Decode(int[] tokens) => NativeDecode(_context, tokens, tokens.Length);

    public int SampleNextToken(float temperature) => NativeSampleNextToken(_context, temperature);

    public string TokenToText(int token) =>
        Marshal.PtrToStringUTF8(NativeTokenToText(_context, token)) ?? string.Empty;

    public bool IsEndOfGeneration(int token) => NativeIsEndOfGeneration(_context, token);

    public void ResetContext() => NativeResetContext(_context);

    public void FreeContext() => NativeFreeContext(_context);

    public void FreeModel() => NativeFreeModel(_model);
}
