namespace TestProject1;

using LeetCode;

internal class LongestSubstringWithoutRepeatingCharactersTests
{
    [Test]
    public void LongestSubstringWithoutRepeatingCharactersTest01()
    {
        var longestSubstringWithoutRepeatingCharacters = new LongestSubstringWithoutRepeatingCharacters();
        Assert.That(longestSubstringWithoutRepeatingCharacters.LengthOfLongestSubstring("abca"), Is.EqualTo(3));
    }

    [Test]
    public void LongestSubstringWithoutRepeatingCharactersTest02()
    {
        var longestSubstringWithoutRepeatingCharacters = new LongestSubstringWithoutRepeatingCharacters();
        Assert.That(longestSubstringWithoutRepeatingCharacters.LengthOfLongestSubstring("abcabcbb"), Is.EqualTo(3));
    }
}
