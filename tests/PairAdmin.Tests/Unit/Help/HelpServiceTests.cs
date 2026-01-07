using PairAdmin.Help;

namespace PairAdmin.Tests.Unit.Help;

/// <summary>
/// Unit tests for HelpService
/// </summary>
public class HelpServiceTests
{
    private readonly HelpService _helpService;

    public HelpServiceTests()
    {
        _helpService = new HelpService();
    }

    [Fact]
    public void GetTopic_WithValidId_ReturnsTopic()
    {
        // Act
        var topic = _helpService.GetTopic("getting-started.intro");

        // Assert
        topic.Should().NotBeNull();
        topic!.Id.Should().Be("getting-started.intro");
        topic.Title.Should().Contain("Introduction");
    }

    [Fact]
    public void GetTopic_WithInvalidId_ReturnsNull()
    {
        // Act
        var topic = _helpService.GetTopic("nonexistent.topic");

        // Assert
        topic.Should().BeNull();
    }

    [Fact]
    public void GetTopic_WithCaseInsensitiveId_ReturnsTopic()
    {
        // Act
        var topic = _helpService.GetTopic("GETTING-STARTED.INTRO");

        // Assert
        topic.Should().NotBeNull();
    }

    [Fact]
    public void GetTopicsByCategory_WithValidCategory_ReturnsTopics()
    {
        // Act
        var topics = _helpService.GetTopicsByCategory("core-commands").ToList();

        // Assert
        topics.Should().NotBeEmpty();
        topics.Should().AllSatisfy(t => t.Category.Should().Be("core-commands"));
    }

    [Fact]
    public void GetTopicsByCategory_WithInvalidCategory_ReturnsEmpty()
    {
        // Act
        var topics = _helpService.GetTopicsByCategory("nonexistent").ToList();

        // Assert
        topics.Should().BeEmpty();
    }

    [Fact]
    public void GetTopicsByCategoryName_WithValidName_ReturnsTopics()
    {
        // Act
        var topics = _helpService.GetTopicsByCategoryName("Core Commands").ToList();

        // Assert
        topics.Should().NotBeEmpty();
    }

    [Fact]
    public void Search_WithMatchingQuery_ReturnsTopics()
    {
        // Act
        var results = _helpService.Search("context").ToList();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(t => t.Title.Contains("Context", StringComparison.OrdinalIgnoreCase) ||
                                       t.Content.Contains("context", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_WithCaseInsensitiveQuery_Works()
    {
        // Act
        var resultsLower = _helpService.Search("context").ToList();
        var resultsUpper = _helpService.Search("CONTEXT").ToList();

        // Assert
        resultsLower.Should().HaveSameCount(resultsUpper);
    }

    [Fact]
    public void Search_WithEmptyQuery_ReturnsEmpty()
    {
        // Act
        var results = _helpService.Search("").ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Search_WithWhitespaceQuery_ReturnsEmpty()
    {
        // Act
        var results = _helpService.Search("   ").ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void GetTutorials_ReturnsAllTutorials()
    {
        // Act
        var tutorials = _helpService.GetTutorials().ToList();

        // Assert
        tutorials.Should().NotBeEmpty();
    }

    [Fact]
    public void GetTutorial_WithValidId_ReturnsTutorial()
    {
        // Act
        var tutorial = _helpService.GetTutorial("getting-started");

        // Assert
        tutorial.Should().NotBeNull();
        tutorial!.Id.Should().Be("getting-started");
        tutorial.Steps.Should().NotBeEmpty();
    }

    [Fact]
    public void GetTutorial_WithInvalidId_ReturnsNull()
    {
        // Act
        var tutorial = _helpService.GetTutorial("nonexistent");

        // Assert
        tutorial.Should().BeNull();
    }

    [Fact]
    public void GetTutorialsByDifficulty_WithBeginner_ReturnsMatchingTutorials()
    {
        // Act
        var tutorials = _helpService.GetTutorialsByDifficulty(TutorialDifficulty.Beginner).ToList();

        // Assert
        tutorials.Should().NotBeEmpty();
        tutorials.Should().AllSatisfy(t => t.Difficulty.Should().Be(TutorialDifficulty.Beginner));
    }

    [Fact]
    public void GetCategories_ReturnsAllCategories()
    {
        // Act
        var categories = _helpService.GetCategories().ToList();

        // Assert
        categories.Should().NotBeEmpty();
        categories.Should().BeInAscendingOrder(c => c.Order);
    }

    [Fact]
    public void GetRelatedTopics_WithValidTopic_ReturnsRelatedTopics()
    {
        // Act
        var related = _helpService.GetRelatedTopics("command.help").ToList();

        // Assert
        related.Should().NotBeEmpty();
    }

    [Fact]
    public void GetRelatedTopics_WithInvalidTopic_ReturnsEmpty()
    {
        // Act
        var related = _helpService.GetRelatedTopics("nonexistent").ToList();

        // Assert
        related.Should().BeEmpty();
    }

    [Fact]
    public void GetCommandHelp_WithValidCommand_ReturnsTopic()
    {
        // Act
        var help = _helpService.GetCommandHelp("context");

        // Assert
        help.Should().NotBeNull();
        help!.Id.Should().Be("command.context");
    }

    [Fact]
    public void GetCommandHelp_WithLeadingSlash_HandlesCorrectly()
    {
        // Act
        var help = _helpService.GetCommandHelp("/context");

        // Assert
        help.Should().NotBeNull();
    }

    [Fact]
    public void GetCommandTopics_ReturnsOnlyCommandTopics()
    {
        // Act
        var topics = _helpService.GetCommandTopics().ToList();

        // Assert
        topics.Should().NotBeEmpty();
        topics.Should().AllSatisfy(t => t.Id.Should().StartWith("command."));
    }

    [Fact]
    public void GetGettingStarted_ReturnsIntroTopic()
    {
        // Act
        var topic = _helpService.GetGettingStarted();

        // Assert
        topic.Should().NotBeNull();
        topic!.Id.Should().Be("getting-started.intro");
    }

    [Fact]
    public void GetTopicCount_ReturnsCorrectCount()
    {
        // Act
        var count = _helpService.GetTopicCount();

        // Assert
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetTutorialCount_ReturnsCorrectCount()
    {
        // Act
        var count = _helpService.GetTutorialCount();

        // Assert
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Search_FiltersByTag()
    {
        // Act
        var results = _helpService.Search("intro").ToList();

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void Topics_AreSortedByPriority()
    {
        // Act
        var topics = _helpService.GetTopicsByCategory("core-commands").ToList();

        // Assert
        topics.Should().BeInDescendingOrder(t => t.Priority);
    }

    [Fact]
    public void Tutorials_AreSortedByDifficulty()
    {
        // Act
        var tutorials = _helpService.GetTutorials().ToList();

        // Assert
        tutorials.Should().BeInAscendingOrder(t => (int)t.Difficulty);
    }

    [Fact]
    public void Tutorial_HasValidSteps()
    {
        // Act
        var tutorial = _helpService.GetTutorial("getting-started");

        // Assert
        tutorial.Should().NotBeNull();
        tutorial!.Steps.Should().NotBeEmpty();
        tutorial.Steps.Select(s => s.StepNumber).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Category_HasRequiredFields()
    {
        // Act
        var categories = _helpService.GetCategories().ToList();

        // Assert
        foreach (var category in categories)
        {
            category.Id.Should().NotBeNullOrEmpty();
            category.Name.Should().NotBeNullOrEmpty();
            category.TopicIds.Should().NotBeNull();
        }
    }
}

/// <summary>
/// Tests for HelpService with logger
/// </summary>
public class HelpServiceWithLoggerTests
{
    [Fact]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        // Arrange
        var logger = new TestLogger<HelpService>();

        // Act & Assert
        var service = new HelpService(logger);
        service.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for HelpContent models
/// </summary>
public class HelpContentTests
{
    [Fact]
    public void HelpTopic_CanBeCreated()
    {
        // Arrange & Act
        var topic = new HelpTopic
        {
            Id = "test.topic",
            Title = "Test Topic",
            Category = "test",
            Priority = 100,
            Tags = new List<string> { "tag1", "tag2" },
            Content = "# Test Content",
            RelatedTopics = new List<string> { "related1", "related2" }
        };

        // Assert
        topic.Id.Should().Be("test.topic");
        topic.Title.Should().Be("Test Topic");
        topic.Tags.Should().HaveCount(2);
        topic.RelatedTopics.Should().HaveCount(2);
    }

    [Fact]
    public void HelpCategory_CanBeCreated()
    {
        // Arrange & Act
        var category = new HelpCategory
        {
            Id = "test-category",
            Name = "Test Category",
            Description = "A test category",
            Icon = "TestIcon",
            Order = 1,
            TopicIds = new List<string> { "topic1", "topic2" }
        };

        // Assert
        category.Id.Should().Be("test-category");
        category.Name.Should().Be("Test Category");
        category.Order.Should().Be(1);
        category.TopicIds.Should().HaveCount(2);
    }

    [Fact]
    public void Tutorial_CanBeCreated()
    {
        // Arrange & Act
        var tutorial = new Tutorial
        {
            Id = "test-tutorial",
            Title = "Test Tutorial",
            Description = "A test tutorial",
            Difficulty = TutorialDifficulty.Beginner,
            EstimatedMinutes = 5,
            Tags = new List<string> { "test" },
            Prerequisites = new List<string>(),
            Steps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    StepNumber = 1,
                    Title = "Step 1",
                    Content = "Do something",
                    Exercise = "Do the thing"
                }
            }
        };

        // Assert
        tutorial.Id.Should().Be("test-tutorial");
        tutorial.Difficulty.Should().Be(TutorialDifficulty.Beginner);
        tutorial.Steps.Should().ContainSingle();
    }

    [Fact]
    public void TutorialDifficulty_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<TutorialDifficulty>().Should().Contain(TutorialDifficulty.Beginner);
        Enum.GetValues<TutorialDifficulty>().Should().Contain(TutorialDifficulty.Intermediate);
        Enum.GetValues<TutorialDifficulty>().Should().Contain(TutorialDifficulty.Advanced);
    }
}
