﻿using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace CSharpGuidelinesAnalyzer.Test.Specs
{
    public sealed class WordsTokenizerSpecs
    {
        [Fact]
        public void When_empty_string_it_must_be_tokenized()
        {
            RunTest("");
        }

        [Fact]
        public void When_single_lowercase_word_it_must_be_tokenized()
        {
            RunTest("first",
                new WordToken("first", WordTokenKind.CamelCaseWord));
        }

        [Fact]
        public void When_single_pascal_cased_word_it_must_be_tokenized()
        {
            RunTest("First",
                new WordToken("First", WordTokenKind.PascalCaseWord));
        }

        [Fact]
        public void When_single_uppercase_word_it_must_be_tokenized()
        {
            RunTest("FIRST",
                new WordToken("FIRST", WordTokenKind.UpperCaseWord));
        }

        [Fact]
        public void When_underscores_it_must_be_tokenized()
        {
            RunTest("_____",
                new WordToken("_____", WordTokenKind.Separators));
        }

        [Fact]
        public void When_two_separated_lowercase_words_it_must_be_tokenized()
        {
            RunTest("first_second",
                new WordToken("first", WordTokenKind.CamelCaseWord),
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("second", WordTokenKind.CamelCaseWord));
        }

        [Fact]
        public void When_lowercase_and_pascal_cased_word_it_must_be_tokenized()
        {
            RunTest("firstSecond",
                new WordToken("first", WordTokenKind.CamelCaseWord),
                new WordToken("Second", WordTokenKind.PascalCaseWord));
        }

        [Fact]
        public void When_separated_lowercase_and_camel_cased_word_it_must_be_tokenized()
        {
            RunTest("first_Second",
                new WordToken("first", WordTokenKind.CamelCaseWord),
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("Second", WordTokenKind.PascalCaseWord));
        }

        [Fact]
        public void When_two_pascal_cased_words_it_must_be_tokenized()
        {
            RunTest("FirstSecond",
                new WordToken("First", WordTokenKind.PascalCaseWord),
                new WordToken("Second", WordTokenKind.PascalCaseWord));
        }

        [Fact]
        public void When_two_separated_pascal_cased_words_it_must_be_tokenized()
        {
            RunTest("First_Second",
                new WordToken("First", WordTokenKind.PascalCaseWord),
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("Second", WordTokenKind.PascalCaseWord));
        }

        [Fact]
        public void When_uppercase_and_pascal_cased_word_it_must_be_tokenized()
        {
            RunTest("FIRSTSecond",
                new WordToken("FIRST", WordTokenKind.UpperCaseWord),
                new WordToken("Second", WordTokenKind.PascalCaseWord));
        }

        [Fact]
        public void When_separated_uppercase_and_pascal_cased_word_it_must_be_tokenized()
        {
            RunTest("FIRST_Second",
                new WordToken("FIRST", WordTokenKind.UpperCaseWord),
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("Second", WordTokenKind.PascalCaseWord));
        }

        [Fact]
        public void When_lowercase_and_uppercase_word_it_must_be_tokenized()
        {
            RunTest("firstSECOND",
                new WordToken("first", WordTokenKind.CamelCaseWord),
                new WordToken("SECOND", WordTokenKind.UpperCaseWord));
        }

        [Fact]
        public void When_separated_lowercase_and_uppercase_word_it_must_be_tokenized()
        {
            RunTest("first_SECOND",
                new WordToken("first", WordTokenKind.CamelCaseWord),
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("SECOND", WordTokenKind.UpperCaseWord));
        }

        [Fact]
        public void When_two_separated_uppercase_words_it_must_be_tokenized()
        {
            RunTest("FIRST_SECOND",
                new WordToken("FIRST", WordTokenKind.UpperCaseWord),
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("SECOND", WordTokenKind.UpperCaseWord));
        }

        [Fact]
        public void When_pascal_cased_word_is_surrounded_by_single_uppercase_letters_it_must_be_tokenized()
        {
            RunTest("AFirstS",
                new WordToken("A", WordTokenKind.UpperCaseWord),
                new WordToken("First", WordTokenKind.PascalCaseWord),
                new WordToken("S", WordTokenKind.UpperCaseWord));
        }

        [Fact]
        public void
            When_pascal_cased_word_is_surrounded_by_single_uppercase_letters_and_underscores_it_must_be_tokenized()
        {
            RunTest("A_First_S",
                new WordToken("A", WordTokenKind.UpperCaseWord),
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("First", WordTokenKind.PascalCaseWord),
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("S", WordTokenKind.UpperCaseWord));
        }

        [Fact]
        public void When_lowercase_word_is_surrounded_by_single_underscores_it_must_be_tokenized()
        {
            RunTest("_first_",
                new WordToken("_", WordTokenKind.Separators),
                new WordToken("first", WordTokenKind.CamelCaseWord),
                new WordToken("_", WordTokenKind.Separators));
        }

        private static void RunTest([NotNull] string text, [NotNull] params WordToken[] expected)
        {
            var tokenizer = new WordsTokenizer(text);
            WordToken[] tokens = tokenizer.GetTokens().ToArray();

            tokens.Should().HaveSameCount(expected);

            for (int index = 0; index < tokens.Length; index++)
            {
                tokens[index].Should().Be(expected[index]);
            }
        }
    }
}
