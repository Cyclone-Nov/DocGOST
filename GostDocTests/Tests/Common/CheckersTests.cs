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

        // TODO: реализовать тест. задать значение меньше 2 символов на входе и получить false
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

        // TODO: реализовать тест. задать значение без символов на входе и получить false
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

        // TODO: реализовать тест. задать значение без цифр после символов на входе и получить false
        [Theory]
        [InlineData("/?*")]
        [InlineData("abc")]
        public void CheckDesignatorFormat_SetValueWithoutDigits_ReturnFalse(string tValue)
        {
            // Act
            bool actual = Checkers.CheckDesignatorFormat(tValue);

            // Assert
            Assert.False(actual);
        }

        // TODO: реализовать тест. задать правильные значения на входе и получить true
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
