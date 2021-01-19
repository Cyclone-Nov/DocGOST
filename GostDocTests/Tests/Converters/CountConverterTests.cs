using System;
using System.Text;
using System.Collections.Generic;
using Xunit;
using GostDOC.ViewModels;
using System.Globalization;
using GostDOC.Converters;
using System.Windows.Data;

namespace GostDOC.Tests.Converters
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
    public class CountConverterTests
    {
        IValueConverter GetDefaultCountConverter()
        {
            return new CountConverter();
        }

        [Fact]
        public void Convert_ValueNull_ReturnEmpty()
        {
            // Arrange
            var converter = GetDefaultCountConverter();
            
            // Act
            var actual = converter.Convert(null, null, null, CultureInfo.CurrentCulture);

            // Assert
            Assert.Equal(actual.ToString(), string.Empty);
        }

        [Fact]
        public void Convert_ValueZero_ReturnEmpty()
        {
            // Arrange
            var converter = GetDefaultCountConverter();

            // Act
            var actual = converter.Convert(0, null, null, CultureInfo.CurrentCulture);

            // Assert
            Assert.Equal(actual.ToString(), string.Empty);
        }

        // TODO: надо проверить max, min, overflow значения для типа Int32. возможно разбить на отдельные функции проверки
        [Theory]
        [InlineData(20)]
        [InlineData(300)]
        public void Convert_ReturnValue(int value)
        {
            // Arrange
            var converter = GetDefaultCountConverter();

            // Act
            var actual = (Int32)converter.Convert(value, null, null, CultureInfo.CurrentCulture);

            // Assert
            Assert.Equal(actual, value);
        }

        //TODO: проверить обратную конвертацию из Int32 в string
        [Fact]
        public void ConvertBack_ReturnEmpty()
        {
            //
            // TODO: Add test logic here
            //
        }

        //TODO: проверить обратную конвертацию
        [Theory]
        [InlineData("30")]
        public void ConvertBack_ReturnValue(string value)
        {
            //
            // TODO: Add test logic here
            //
        }
    }
}
