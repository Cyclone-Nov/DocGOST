using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using GostDOC.Common;


namespace GostDocTests.Tests.Common
{    
    /// <summary>
    /// test for Converters.CountConverter class
    /// tests create by rules and description in next sources: 
    /// https://docs.microsoft.com/ru-ru/dotnet/core/testing/unit-testing-best-practices
    /// and
    /// https://xunit.net/docs/getting-started/netfx/visual-studio
    /// https://habr.com/ru/post/357648/
    /// https://habr.com/ru/post/262435/
    /// 
    /// </summary>    
    public class CheckersTests
    {

        /// <summary>
        ///  задаем пустое значение на входе и получаем false
        /// </summary>
        [Fact]
        public void CheckDesignatorFormat_SetEmpty_ReturnFalse()
        {
            // Act
            bool actual = Checkers.CheckDesignatorFormat(string.Empty);

            // Assert            
            Assert.False(actual);
        }

        // задаем значение меньше 2 символов на входе и получаем false
        [Theory]
        [InlineData("A")]
        [InlineData("1")]
        public void CheckDesignatorFormat_SetLess2LengthValue_ReturnFalse(string tValue)
        {
            // Act
            bool actual = Checkers.CheckDesignatorFormat(tValue);

            
            // Assert            
            Assert.False(actual);

        }

        // задаем значение без символов на входе и получаем false
        [Theory]
        [InlineData("000")]
        [InlineData("123")]
        public void CheckDesignatorFormat_SetValueWithoutSymbols_ReturnFalse(string tValue)
        {
            // Act
            bool actual = Checkers.CheckDesignatorFormat(tValue);

            // Assert
            Assert.False(actual);
        }

        // задаем значение без цифр после символов на входе и получаем false
        [Fact]
        public void CheckDesignatorFormat_SetValueWithoutDigits_ReturnFalse()
        {
            // Act
            bool actual = Checkers.CheckDesignatorFormat("abc");

            // Assert
            Assert.False(actual);
        }

        // задаем значение состоящее из спец символов и числа на входе и получаем false
        [Fact]
        public void CheckDesignatorFormat_SetSpecialSymbolsAndDigit_ReturnFalse()
        {
            // Act
            bool actual = Checkers.CheckDesignatorFormat("/?*1");

            // Assert
            Assert.False(actual);
        }

        // задаем правильные значения на входе и получаем true
        [Theory]
        [InlineData("A1")]
        [InlineData("VD23")]
        [InlineData("BMN456")]
        public void CheckDesignatorFormat_SetTrueValues_ReturnTrue(string tValue)
        {
            // Arrange


            // Act
            bool actual = Checkers.CheckDesignatorFormat(tValue);

            // Assert
            Assert.True(actual);
        }

    }
}
