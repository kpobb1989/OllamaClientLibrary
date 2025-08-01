using OllamaClientLibrary.Constants;
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
            _client = new OllamaClient(new OllamaOptions()
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
            var response = await _client.GetCompletionAsync("Hi, how are you doing?");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response?.Content, Is.Not.Null);
                Assert.That(response?.Content, Is.Not.Empty);
                Assert.That(response?.Content, Is.Not.WhiteSpace);
            });
        }

        [Test]
        public async Task GetJsonCompletionAsync_ListOfPlanets_ShouldReturnAtLeastOnePlanet()
        {
            // Act
            var response = await _client.GetJsonCompletionAsync<PlanetResponse>("Please provide a list of all the planet names in our solar system. The list should include Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, and Neptune.");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response, Is.Not.Null);
                Assert.That(response?.Data.All(s => !string.IsNullOrWhiteSpace(s.PlanetName)), Is.True);
                Assert.That(response?.Data.Count(), Is.GreaterThan(0));
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
        public async Task GetChatCompletionAsync_KeepConversationHistory_ShouldStoreConversationHistory()
        {
            // Act
            await foreach (var chunk in _client.GetChatCompletionAsync("hello"))
            {
                Assert.That(chunk, Is.Not.Null);
            }

            // Assert
            Assert.That(_client.ConversationHistory, Is.Not.Empty);
        }

        [Test]
        public async Task GetChatCompletionAsync_MultiplePrompts_ShouldStoreThemInConversationHistory()
        {
            // Arrange
            _client = new OllamaClient(new OllamaOptions()
            {
                SystemPrompt = null
            });
            var conversation = new[] { "hello", "how are you doing?" };

            // Act
            foreach (var prompt in conversation)
            {
                await foreach (var _ in _client.GetChatCompletionAsync(prompt))
                {
                }
            }

            // Assert
            Assert.That(_client.ConversationHistory.Count(s => s.Role == MessageRole.User), Is.EqualTo(conversation.Length));
        }

        [Test]
        public async Task GetChatCompletionAsync_MultiplePrompts_ShouldReturnCorrespondingAmountOfCompletions()
        {
            // Arrange
            var prompts = new[]
            {
                "hello",
                "how are you doing?"
            };

            // Act
            foreach (var prompt in prompts)
            {
                await foreach (var _ in _client.GetChatCompletionAsync(prompt))
                {
                }
            }

            // Assert
            Assert.That(_client.ConversationHistory.Count(s => s.Role == MessageRole.Assistant), Is.EqualTo(prompts.Length));
        }

        [Test]
        public async Task GetChatCompletionAsync_MultiplePrompts_ShouldKeepThemInConversationHistory()
        {
            // Arrange
            var prompts = new[] {
                "hi",
                "how are you doing?",
                "I'm good, thanks",
                "I see"
            };

            // Act
            foreach (var prompt in prompts)
            {
                await foreach (var _ in _client.GetChatCompletionAsync(prompt))
                {
                }
            }

            // Assert
            Assert.That(_client.ConversationHistory.Where(s => s.Role != MessageRole.System).ToList(), Has.Count.EqualTo(prompts.Length * 2));
        }

        [Test]
        public async Task GetChatCompletionAsync_WithCancelledToken_ShouldTerminateConversation()
        {
            // Arrange
            _client = new OllamaClient(new OllamaOptions()
            {
                Model = Model,
                SystemPrompt = null
            });

            var cts = new CancellationTokenSource();

            // Act
            await foreach (var unused in _client.GetChatCompletionAsync("hi", ct: cts.Token))
            {
                await cts.CancelAsync();
            }

            // Assert
            Assert.That(_client.ConversationHistory.Where(s => s.Role != MessageRole.System).ToList(), Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetTextCompletionAsync_WithTools_ShouldReturnTemperature()
        {
            // Arrange
            _client = new(new OllamaOptions()
            {
                Model = Model,
                Tools = ToolFactory.Create<WeatherService>()
            });

            // Act
            var response = await _client.GetCompletionAsync("What is the weather today in Paris?");

            // Assert
            Assert.That(response?.Content, Is.Not.Null);
        }

        record PlanetResponse(IEnumerable<Planet> Data);

        record Planet(string PlanetName);
    }
}
