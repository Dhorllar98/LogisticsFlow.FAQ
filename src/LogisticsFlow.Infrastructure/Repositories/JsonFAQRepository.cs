using System.Text.Json;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;

namespace LogisticsFlow.Infrastructure.Repositories;

public class JsonFAQRepository : IFAQRepository
{
    private readonly string _filePath;
    private List<FAQEntry>? _cachedEntries;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public JsonFAQRepository()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "data", "faq_knowledgebase.json");
    }

    public async Task<IReadOnlyList<FAQEntry>> GetAllAsync()
    {
        if (_cachedEntries is not null) return _cachedEntries;

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedEntries is not null) return _cachedEntries;

            if (!File.Exists(_filePath))
                throw new KnowledgeBoundaryException($"FAQ knowledge base file not found at '{_filePath}'.");

            var json = await File.ReadAllTextAsync(_filePath);
            var rawEntries = JsonSerializer.Deserialize<List<RawFAQEntry>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (rawEntries is null || rawEntries.Count == 0)
                throw new KnowledgeBoundaryException("FAQ knowledge base file was found but contained no entries.");

            _cachedEntries = rawEntries.Select(MapToEntity).ToList();
            return _cachedEntries;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<IReadOnlyList<FAQEntry>> GetByCategoryAsync(LogisticCategory category)
    {
        var all = await GetAllAsync();
        return all.Where(e => e.Category == category).ToList();
    }

    private static FAQEntry MapToEntity(RawFAQEntry raw)
    {
        if (!Enum.TryParse<LogisticCategory>(raw.Category, ignoreCase: true, out var category))
            throw new KnowledgeBoundaryException($"Knowledge base entry '{raw.Id}' has an unrecognized category: '{raw.Category}'.");

        return new FAQEntry { Id = raw.Id, Category = category, Question = raw.Question, Answer = raw.Answer };
    }

    private sealed record RawFAQEntry(string Id, string Category, string Question, string Answer);
}