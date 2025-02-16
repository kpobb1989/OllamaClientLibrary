using OllamaClientLibrary.Cache;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.IntegrationTests.Tools;
using OllamaClientLibrary.Models;
using OllamaClientLibrary.Tools;

namespace OllamaClientLibrary.IntegrationTests
{
    public class OllamaClientLibraryIntegrationTests
    {
        private const string Model = "qwen2.5:1.5b";

        private OllamaClient _client;

        [SetUp]
        public void Setup()
        {
            _client = new(new OllamaOptions()
            {
                Model = Model
            });
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }

        [Test]
        public async Task GetTextCompletionAsync_SimplePrompt_ShouldReturnTextCompletion()
        {
            // Act
            var response = await _client.GetTextCompletionAsync("Hi, how are you doing?");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response, Is.Not.Null);
                Assert.That(response, Is.Not.Empty);
                Assert.That(response, Is.Not.WhiteSpace);
            });
        }

        [Test]
        public async Task GetJsonCompletionAsync_ListOfPlanets_ShouldReturnEightPlanetNames()
        {
            // Act
            var response = await _client.GetJsonCompletionAsync<PlanetResponse> ("Please provide a list of all the planet names in our solar system. The list should include Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, and Neptune.");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response, Is.Not.Null);
                Assert.That(response?.Data?.All(s => !string.IsNullOrWhiteSpace(s.PlanetName)), Is.True);
                Assert.That(response?.Data?.Count(), Is.GreaterThan(0));
            });
        }

        [Test]
        public async Task GetChatCompletionAsync_SimplePrompt_ShouldReturnChatCompletions()
        {
            // Act & Assert
            await foreach (var chunk in _client.GetChatCompletionAsync("hello"))
            {
                Assert.That(chunk, Is.Not.Null);
            }
        }

        [Test]
        public async Task GetChatCompletionAsync_KeepChatHistoryTrue_ShouldStoreChatHistory()
        {
            // Arrange
            _client = new(new OllamaOptions()
            {
                Model = Model,
                KeepChatHistory = true
            });

            // Act
            await foreach (var chunk in _client.GetChatCompletionAsync("hello"))
            {
                Assert.That(chunk, Is.Not.Null);
            }

            // Assert
            Assert.That(_client.ChatHistory, Is.Not.Empty);
        }

        [Test]
        public async Task GetChatCompletionAsync_KeepChatHistoryFalse_ShouldNotStoreChatHistory()
        {
            // Arrange
            _client = new(new OllamaOptions()
            {
                Model = Model,
                KeepChatHistory = false
            });

            // Act
            await foreach (var chunk in _client.GetChatCompletionAsync("hello"))
            {
                Assert.That(chunk, Is.Not.Null);
            }

            // Assert
            Assert.That(_client.ChatHistory.Count, Is.EqualTo(0));
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
            await foreach (var _ in _client.GetChatCompletionAsync("hi", ct: cts.Token))
            {
                cts.Cancel();
            }

            // Assert
            Assert.That(_client.ChatHistory, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetTextCompletionAsync_WithTools_ShouldResponseTemperature()
        {
            // Arrange
            var tool = ToolFactory.Create<Weather>(nameof(Weather.GetTemperature));

            // Act
            var response = await _client.GetTextCompletionAsync("What is the weather today in Paris?", tool);

            // Assert
            Assert.That(response, Is.EquivalentTo("23"));
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
        public async Task ListModelsAsync_RemoteModels_ShouldReturnCachedModels()
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
                Assert.That(models.Count(), Is.EqualTo(cache?.Count()));
                Assert.That(models.IntersectBy(cache!.Select(s => (s.Name, s.ModifiedAt, s.Size)), s => (s.Name, s.ModifiedAt, s.Size)).Count(), Is.EqualTo(models.Count()));
            });
        }

        [Test]
        public async Task ListModelsAsync_FilteringOnRemoteModels_ShouldReturnCachedModels()
        {
            // Act
            var models = await _client.ListModelsAsync("deepseek-r1");

            // Assert
            Assert.That(models, Is.Not.Null);
            Assert.That(models.Count(), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task ListModelsAsync_ModelSizeTiny_ShouldReturnModelsLessThanOrEqualTo500Mb()
        {
            // Act
            var models = await _client.ListModelsAsync(size: ModelSize.Tiny);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(models, Is.Not.Null);

                Assert.That(models.All(s => s.Size <= SizeConverter.GigabytesToBytes(0.5)), Is.True);
            });
        }

        [Test]
        public async Task ListModelsAsync_ModelSizeSmall_ShouldReturnModelsMoreThan500MbANdLessThanOrEqualTo2Gb()
        {
            // Act
            var models = await _client.ListModelsAsync(size: ModelSize.Small);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(models, Is.Not.Null);

                Assert.That(models.All(s => s.Size > SizeConverter.GigabytesToBytes(0.5) && s.Size <= SizeConverter.GigabytesToBytes(2)), Is.True);
            });
        }

        [Test]
        public async Task ListModelsAsync_ModelSizeMedium_ShouldReturnModelsMoreThan2GbAndLessThanOrEqualTo5Gb()
        {
            // Act
            var models = await _client.ListModelsAsync(size: ModelSize.Medium);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(models, Is.Not.Null);

                Assert.That(models.All(s => s.Size > SizeConverter.GigabytesToBytes(2) && s.Size <= SizeConverter.GigabytesToBytes(5)), Is.True);
            });
        }

        [Test]
        public async Task ListModelsAsync_ModelSizeLarge_ShouldReturnModelsMoreThan5Gb()
        {
            // Act
            var models = await _client.ListModelsAsync(size: ModelSize.Large);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(models, Is.Not.Null);
                Assert.That(models.All(s => s.Size > SizeConverter.GigabytesToBytes(5)), Is.True);
            });
        }

        [TestCase(Model, ModelSize.Small, ModelLocation.Local)]
        [TestCase(Model, ModelSize.Small, ModelLocation.Remote)]
        public async Task ListModelsAsync_ComplexFilter_ShouldReturnAtLeastOneModel(string pattern, ModelSize size, ModelLocation location)
        {
            // Act
            var models = await _client.ListModelsAsync(
                pattern: pattern,
                size: size,
                location: location);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(models, Is.Not.Null);
                Assert.That(models.Count(), Is.GreaterThanOrEqualTo(1));
            });
        }

        record PlanetResponse(IEnumerable<Planet> Data);

        record Planet(string PlanetName);
    }
}
