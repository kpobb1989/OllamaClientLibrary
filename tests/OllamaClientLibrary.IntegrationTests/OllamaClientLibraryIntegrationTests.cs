﻿using OllamaClientLibrary.Cache;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto.Models;

namespace OllamaClientLibrary.IntegrationTests
{
    public class OllamaClientLibraryIntegrationTests
    {
        private OllamaClient _client;

        [SetUp]
        public void Setup()
        {
            _client = new(new LocalOllamaOptions()
            {
                Model = "llama3.1:8b"
            });
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }

        [Test]
        public async Task GenerateCompletionTextAsync_SimplePrompt_ShouldReturnTextCompletion()
        {
            // Act
            var response = await _client.GenerateCompletionTextAsync("Hi, how are you doing?");

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response, Is.Not.Empty);
        }

        [Test]
        public async Task GenerateCompletionTextAsync_ListOfPlanets_ShouldReturnEightPlanetNames()
        {
            // Act
            var response = await _client.GenerateCompletionJsonAsync<PlanetsResponse>("Return a list of all the planet names we have in our solar system");

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Planets, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response.Planets.All(s => !string.IsNullOrWhiteSpace(s.PlanetName)), Is.True);
                Assert.That(response.Planets.Count(), Is.EqualTo(8));
            });
        }

        [Test]
        public async Task GetChatCompletionAsync_SimplePrompt_ShouldReturnChatCompletions()
        {
            // Act
            await foreach (var chunk in _client.GetChatCompletionAsync("hello"))
            {
                // Assert
                Assert.That(chunk, Is.Not.Null);
            }
        }

        [Test]
        public async Task GetChatCompletionAsync_MultiplePrompts_ShouldStoreThemInChatHistory()
        {
            // Arrange
            var prompts = new[] { "hello", "how are you doing?" };

            // Act
            foreach (var prompt in prompts)
            {
                await foreach (var _ in _client.GetChatCompletionAsync(prompt))
                {
                }
            }

            // Assert
            Assert.Multiple(() =>
            {
                var history = _client.ChatHistory.Where(s => s.Role == MessageRole.User);

                Assert.That(history.Count(), Is.EqualTo(prompts.Length));
                Assert.That(history.IntersectBy(prompts, s => s.Content).Count(), Is.EqualTo(prompts.Length));
            });
        }

        [Test]
        public async Task GetChatCompletionAsync_MultiplePrompts_ShouldReturnCorrespondingAmountOfCompletions()
        {
            // Arrange
            var prompts = new[] { "hello", "how are you doing?" };

            // Act
            foreach (var prompt in prompts)
            {
                await foreach (var _ in _client.GetChatCompletionAsync(prompt))
                {
                }
            }

            // Assert
            Assert.That(_client.ChatHistory.Where(s => s.Role == MessageRole.Assistant).Count(), Is.EqualTo(prompts.Length));
        }

        [Test]
        public async Task GetChatCompletionAsync_MultiplePrompts_ShouldKeepThemInChatHistory()
        {
            // Arrange
            var prompts = new[] { "hi", "how are you doing?", "I'm good, thanks", "I see" };

            // Act
            foreach (var prompt in prompts)
            {
                await foreach (var _ in _client.GetChatCompletionAsync(prompt))
                {
                }
            }

            // Assert
            Assert.That(_client.ChatHistory, Has.Count.EqualTo(prompts.Length * 2));
        }

        [Test]
        public async Task GetChatCompletionAsync_WithCancelledToken_ShouldTerminateConversation()
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            await foreach (var _ in _client.GetChatCompletionAsync("hi", cts.Token))
            {
                cts.Cancel();
            }

            // Assert
            Assert.That(_client.ChatHistory, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task ListModelsAsync_LocalModels_ShouldReturnAtLeastOneModel()
        {
            // Act
            var models = await _client.ListModelsAsync(location: ModelLocation.Local);

            // Assert
            Assert.That(models.Count(), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task ListModelsAsync_LocalModels_ShouldReturnModelsWithNonEmptyFields()
        {
            // Act
            var models = await _client.ListModelsAsync(location: ModelLocation.Local);

            // Assert
            Assert.That(models.All(s => !string.IsNullOrEmpty(s.Name) && s.ModifiedAt != null && s.Size != null), Is.True);
        }

        [Test]
        public async Task ListModelsAsync_RemoteModels_ShouldReturnAtLeastOneModel()
        {
            // Act
            var models = await _client.ListModelsAsync(location: ModelLocation.Remote);

            // Assert
            Assert.That(models.Count(), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task ListModelsAsync_RemoteModels_ShouldReturnModelsWithNonEmptyFields()
        {
            // Act
            var models = await _client.ListModelsAsync(location: ModelLocation.Remote);

            // Assert
            Assert.That(models.All(s => !string.IsNullOrEmpty(s.Name) && s.ModifiedAt != null && s.Size != null), Is.True);
        }

        [Test]
        public async Task ListModelsAsync_RemoteModels_ShouldStoreModelsInCache()
        {
            // Arrange
            CacheStorage.Clear();

            // Act
            await _client.ListModelsAsync(location: ModelLocation.Remote);
            var cache = CacheStorage.Get<IEnumerable<Model>>("remote-models");

            // Assert
            Assert.That(cache?.Count(), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task ListModelsAsync_RemoteModels_ShouldReturnCechedModels()
        {
            // Arrange
            CacheStorage.Clear();

            // Act
            var models = await _client.ListModelsAsync(location: ModelLocation.Remote);
            var cache = CacheStorage.Get<IEnumerable<Model>>("remote-models");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(models, Is.Not.Null);
                Assert.That(cache, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(models?.Count(), Is.EqualTo(cache?.Count()));
                Assert.That(models.IntersectBy(cache.Select(s => (s.Name, s.ModifiedAt, s.Size)), s => (s.Name, s.ModifiedAt, s.Size)).Count(), Is.EqualTo(models.Count()));
            });
        }

        record PlanetsResponse(IEnumerable<Planet> Planets);
        record Planet(string PlanetName);
    }
}
