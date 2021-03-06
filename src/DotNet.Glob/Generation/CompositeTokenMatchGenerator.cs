using System;
using System.Collections.Generic;
using DotNet.Globbing.Token;
using System.Text;

namespace DotNet.Globbing.Generation
{
    public class CompositeTokenMatchGenerator : IMatchGenerator, IGlobTokenVisitor
    {
        private readonly IGlobToken[] _tokens;
        private readonly List<IMatchGenerator> _generators;
        private readonly Random _random;
        private int _currentTokenIndex;
        private bool _finished;

        public CompositeTokenMatchGenerator(Random random, IGlobToken[] tokens)
        {
            _random = random;
            _tokens = tokens;
            _generators = new List<IMatchGenerator>();
            _currentTokenIndex = 0;
            foreach (var token in _tokens)
            {
                token.Accept(this);
                if (_finished)
                { // stop visiting any more. Usually happens if encountering wildcard.
                    break;
                }
                _currentTokenIndex = _currentTokenIndex + 1;
            }
        }

        public void AppendMatch(StringBuilder builder)
        {
            foreach (var generator in _generators)
            {
                generator.AppendMatch(builder);
            }
        }

        public void AppendNonMatch(StringBuilder builder)
        {
            foreach (var generator in _generators)
            {
                generator.AppendNonMatch(builder);
            }
        }

        void IGlobTokenVisitor.Visit(CharacterListToken token)
        {
            AddMatchGenerator(new CharacterListTokenMatchGenerator(token, _random));
        }

        void IGlobTokenVisitor.Visit(PathSeparatorToken token)
        {
            AddMatchGenerator(new PathSeparatorMatchGenerator(token, _random));
        }

        void IGlobTokenVisitor.Visit(LiteralToken token)
        {
            AddMatchGenerator(new LiteralTokenMatchGenerator(token, _random));
        }

        void IGlobTokenVisitor.Visit(LetterRangeToken token)
        {
            AddMatchGenerator(new LetterRangeTokenMatchGenerator(token, _random));
        }
        void IGlobTokenVisitor.Visit(NumberRangeToken token)
        {
            AddMatchGenerator(new NumberRangeTokenMatchGenerator(token, _random));
        }

        void IGlobTokenVisitor.Visit(AnyCharacterToken token)
        {
            AddMatchGenerator(new AnyCharacterTokenMatchGenerator(token, _random));
        }

        void IGlobTokenVisitor.Visit(WildcardToken token)
        {
            // if no more tokens then just return as * matches the rest of the segment, and therefore no more matching.
            int remainingCount = _tokens.Length - (_currentTokenIndex + 1);
           
            // Add a nested CompositeTokenEvaluator, passing all of our remaining tokens to it.
            IGlobToken[] remaining = new IGlobToken[remainingCount];
            Array.Copy(_tokens, _currentTokenIndex + 1, remaining, 0, remainingCount);
            var subEvaluator = new WildcardTokenMatchGenerator(token, _random, new CompositeTokenMatchGenerator(_random, remaining));
            AddMatchGenerator(subEvaluator);

            _finished = true; // signlas to stop visiting any further tokens as we have offloaded them all to the nested evaluator.

        }

        void IGlobTokenVisitor.Visit(WildcardDirectoryToken token)
        {
            // if no more tokens then just return as * matches the rest of the segment, and therefore no more matching.
            int remainingCount = _tokens.Length - (_currentTokenIndex + 1);
           
            // Add a nested CompositeTokenEvaluator, passing all of our remaining tokens to it.
            IGlobToken[] remaining = new IGlobToken[remainingCount];
            Array.Copy(_tokens, _currentTokenIndex + 1, remaining, 0, remainingCount);
            var subEvaluator = new WildcardDirectoryTokenMatchGenerator(token, _random, new CompositeTokenMatchGenerator(_random, remaining));
            AddMatchGenerator(subEvaluator);

            _finished = true; // signlas to stop visiting any further tokens as we have offloaded them all to the nested evaluator.

        }

        protected void AddMatchGenerator(IMatchGenerator evaluator)
        {
            _generators.Add(evaluator);
           
        }
    }
}