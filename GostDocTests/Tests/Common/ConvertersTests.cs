using System;
using System.Text;
using System.Collections.Generic;
using Xunit;
using System.Globalization;
using GostDOC.Common;
//using System.Windows.Data;

namespace GostDOC.Tests.Common
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
    public class ConvertersTests
    {    

        [Fact]
        public void GetDocumentCode_SetBill_ReturnBillString()
        {
            // Arrange
            DocType input = DocType.Bill;
            string expected = @"ВП";

            // Act
            var actual = GostDOC.Common.Converters.GetDocumentCode(input);

            // Assert
            Assert.Equal(actual, expected);
        }

        // TODO: сделать тоже самое что и для ВП
        [Fact]
        public void GetDocumentCode_SetItemsList_ReturnItemListString()
        {
  
            // Arrange
            DocType input = DocType.ItemsList;
            string expected = @"ПЭ3";

            // Act
            var actual = GostDOC.Common.Converters.GetDocumentCode(input);

            // Assert
            Assert.Equal(actual, expected);
        }

        // TODO: GetDocumentCode: повторить тесты для Д27 и спецификации, проверить граничные значения


        // TODO: проверки для функции SplitDesignatorToStringAndNumber
        [Fact]
        public void SplitDesignatorToStringAndNumber_SetEmpty_ReturnFalse()
        {
            throw new NotImplementedException();
            // Arrange


            // Act


            // Assert

        }
        
    }
}
