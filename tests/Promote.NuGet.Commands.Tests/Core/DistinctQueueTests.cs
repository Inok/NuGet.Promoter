using FluentAssertions;
using NUnit.Framework;
using Promote.NuGet.Commands.Core;

namespace Promote.NuGet.Commands.Tests.Core;

[TestFixture]
public class DistinctQueueTests
{
    [Test]
    public void Enqueue_and_dequeue_items()
    {
        var queue = new DistinctQueue<string>();

        queue.Enqueue("str_1").Should().BeTrue();
        queue.Enqueue("str_2").Should().BeTrue();

        queue.TryDequeue(out var result).Should().BeTrue();
        result.Should().Be("str_1");

        queue.TryDequeue(out result).Should().BeTrue();
        result.Should().Be("str_2");

        queue.TryDequeue(out result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Test]
    public void Cannot_enqueue_an_item_that_is_already_in_queue()
    {
        var queue = new DistinctQueue<string>();

        queue.Enqueue("str_1").Should().BeTrue();
        queue.Enqueue("str_2").Should().BeTrue();

        queue.Enqueue("str_1").Should().BeFalse();

        queue.TryDequeue(out var result).Should().BeTrue();
        result.Should().Be("str_1");

        queue.TryDequeue(out result).Should().BeTrue();
        result.Should().Be("str_2");

        queue.TryDequeue(out result).Should().BeFalse();
    }

    [Test]
    public void Cannot_enqueue_an_item_that_was_in_queue()
    {
        var queue = new DistinctQueue<string>();

        queue.Enqueue("str_1").Should().BeTrue();
        queue.Enqueue("str_2").Should().BeTrue();

        queue.TryDequeue(out var result).Should().BeTrue();
        result.Should().Be("str_1");

        queue.TryDequeue(out result).Should().BeTrue();
        result.Should().Be("str_2");

        queue.TryDequeue(out result).Should().BeFalse();

        queue.Enqueue("str_1").Should().BeFalse();
        queue.Enqueue("str_2").Should().BeFalse();
        queue.TryDequeue(out result).Should().BeFalse();
    }

    [Test]
    public void HasItems_shows_that_queue_is_not_empty()
    {
        var queue = new DistinctQueue<string>();

        queue.HasItems.Should().BeFalse();

        queue.Enqueue("str_1").Should().BeTrue();

        queue.HasItems.Should().BeTrue();

        queue.TryDequeue(out var result).Should().BeTrue();

        queue.HasItems.Should().BeFalse();
    }

    [Test]
    public void Create_queue_with_items()
    {
        var queue = new DistinctQueue<string>(new[] { "str_1", "str_2" });

        queue.TryDequeue(out var result).Should().BeTrue();
        result.Should().Be("str_1");

        queue.TryDequeue(out result).Should().BeTrue();
        result.Should().Be("str_2");

        queue.TryDequeue(out result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Test]
    public void Ctor_keeps_only_unique_initial_items()
    {
        var queue = new DistinctQueue<string>(new[] { "str_1", "str_2", "str_1" });

        queue.TryDequeue(out var result).Should().BeTrue();
        result.Should().Be("str_1");

        queue.TryDequeue(out result).Should().BeTrue();
        result.Should().Be("str_2");

        queue.TryDequeue(out result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Test]
    public void Cannot_enqueue_items_added_by_ctor()
    {
        var queue = new DistinctQueue<string>(new[] { "str_1", "str_2" });

        queue.TryDequeue(out var result).Should().BeTrue();
        result.Should().Be("str_1");

        queue.Enqueue("str_1").Should().BeFalse();
        queue.Enqueue("str_2").Should().BeFalse();

        queue.TryDequeue(out result).Should().BeTrue();
        result.Should().Be("str_2");

        queue.TryDequeue(out result).Should().BeFalse();
        result.Should().BeNull();
    }
}