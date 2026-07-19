using System;
using Xunit;
using MyPortfolio.Web.Infrastructure;

namespace MyPortfolio.Tests
{
    public class LrcParserTests
    {
        [Fact]
        public void Parse_WithNullOrEmpty_ShouldReturnEmptyList()
        {
            var resultNull = LrcParser.Parse(null);
            var resultEmpty = LrcParser.Parse("   ");

            Assert.Empty(resultNull);
            Assert.Empty(resultEmpty);
        }

        [Fact]
        public void Parse_WithValidStandardLrc_ShouldParseCorrectly()
        {
            var lrc = "[01:15.30] Dòng nhạc thứ nhất\n[02:05.00] Dòng nhạc thứ hai";
            var result = LrcParser.Parse(lrc);

            Assert.Equal(2, result.Count);
            
            Assert.Equal(75.3, result[0].Time); // 1*60 + 15 + 0.30
            Assert.Equal("Dòng nhạc thứ nhất", result[0].Text);

            Assert.Equal(125.0, result[1].Time); // 2*60 + 5 + 0.00
            Assert.Equal("Dòng nhạc thứ hai", result[1].Text);
        }

        [Fact]
        public void Parse_WithColonAsMsSeparator_ShouldParseCorrectly()
        {
            var lrc = "[00:45:123] Test milliseconds with colon";
            var result = LrcParser.Parse(lrc);

            Assert.Single(result);
            Assert.Equal(45.123, result[0].Time);
            Assert.Equal("Test milliseconds with colon", result[0].Text);
        }

        [Fact]
        public void Parse_WithoutMilliseconds_ShouldParseCorrectly()
        {
            var lrc = "[03:40] Lời bài hát không có mili giây";
            var result = LrcParser.Parse(lrc);

            Assert.Single(result);
            Assert.Equal(220.0, result[0].Time); // 3*60 + 40
            Assert.Equal("Lời bài hát không có mili giây", result[0].Text);
        }

        [Fact]
        public void Parse_WithInvalidLines_ShouldIgnoreInvalidLines()
        {
            var lrc = "Tiêu đề bài hát: Test Title\n[00:10.50] Lời 1\n[Invalid LRC] Lời 2\n[00:20] Lời 3";
            var result = LrcParser.Parse(lrc);

            Assert.Equal(2, result.Count);
            
            Assert.Equal(10.5, result[0].Time);
            Assert.Equal("Lời 1", result[0].Text);

            Assert.Equal(20.0, result[1].Time);
            Assert.Equal("Lời 3", result[1].Text);
        }
    }
}
