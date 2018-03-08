using OMnG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace UnitTest
{
    public class ObjectExtensionsTests
    {
        #region nested types

        public class Test1
        {
            public int Value { get; set; }
            public string ValueString { get; set; }
            public Test2 Test { get; set; }
            public List<Test2> Tests { get; set; }
        }
        public class Test2
        {
            public int Value { get; set; }
            public string ValueString { get; set; }
        }

        #endregion

        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "ToPropertyNameCollection")]
        public void ToPropertyNameCollection()
        {
            Expression<Func<Test1, object>> expr;

            expr = p => p.Value;
            Assert.Equal(new string[] { "Value" }, expr.ToPropertyNameCollection());

            expr = p => new { p.Value, p.ValueString };
            Assert.Equal(new string[] { "Value", "ValueString" }, expr.ToPropertyNameCollection());
        }

        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "HasPropery")]
        public void HasPropery()
        {
            Test1 test = new Test1();

            Assert.True(test.HasPropery("Value"));
            Assert.False(test.HasPropery("Value1"));
            Assert.True(test.HasPropery<int>("Value"));
            Assert.False(test.HasPropery<string>("Value"));
        }

        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "Get_Set_PropValue")]
        public void Get_Set_PropValue()
        {
            Test1 test = new Test1() { Value = 1, ValueString = "test", Test = new Test2(), Tests = new List<Test2>() { new Test2(), new Test2() } };

            Assert.True(test.GetPropValue<Test1, int>("Value") == 1);
            Assert.True((int)test.GetPropValue("Value") == 1);

            test.SetPropValue<int>("Value", 2);
            Assert.True(test.GetPropValue<Test1, int>("Value") == 2);
            Assert.True((int)test.GetPropValue("Value") == 2);
        }

        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "Include_Exlude_Properties")]
        public void Include_Exlude_Properties()
        {
            Test1 test = new Test1() { Value = 1, ValueString = "test", Test = new Test2(), Tests = new List<Test2>() { new Test2(), new Test2() } };

            Assert.Equal(new Dictionary<string, object>() { { "Value", 1 } }, test.SelectProperties(p => p.Value));
            Assert.Equal(new Dictionary<string, object>() { { "Value", 1 }, { "ValueString", "test" } }, test.SelectProperties(p => new { p.Value, p.ValueString }));

            Assert.Equal(new string[] { "ValueString", "Test", "Tests" }, test.ExludeProperties(p => p.Value).Keys);
            Assert.Equal(new Dictionary<string, object>() { { "Value", 1 } }, test.ExludeProperties(p => new { p.ValueString, p.Test, p.Tests }));

            Assert.Equal(new string[] { "Test", "Tests" }, test.ExludeProperties<Test1, Test2>().Keys);
            Assert.Equal(new string[] { "Test", "Tests" }, test.ExludeProperties(new Test2().GetType()).Keys);

            Assert.Equal(new string[] { "Test" }, test.ExludeProperties<Test1, Test2>(p => p.Tests).Keys);
            Assert.Equal(new Dictionary<string, object>() { { "Value", 1 }, { "ValueString", "test" }, { "Test", test.Test } }, test.SelectProperties<Test1, Test2>(p => p.Test));
        }
        
        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "Include_Exlude_Properties_ByType")]
        public void Include_Exlude_Properties_ByType()
        {
            Test1 test = new Test1() { Value = 1, ValueString = "test", Test = new Test2(), Tests = new List<Test2>() { new Test2(), new Test2() } };

            Assert.Equal(new Dictionary<string, object>() { { "Value", 1 } }, test.SelectTypesProperties(typeof(int)));
            Assert.Equal(new Dictionary<string, object>() { { "Value", 1 }, { "ValueString", "test" } }, test.SelectPrimitiveTypesProperties());

            Assert.Equal(new string[] { "ValueString", "Test", "Tests" }, test.ExludeTypesProperties(typeof(int)).Keys);

            Assert.Equal(new string[] { "Test", "Tests" }, test.ExludePrimitiveTypesProperties().Keys);
            Assert.Equal(new string[] { "Tests" }, test.SelectCollectionTypesProperties().Keys);

            Assert.Equal(new string[] { "Value", "ValueString", "Test" }, test.ExludeCollectionTypesProperties().Keys);
        }

        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "CopyProperties")]
        public void CopyProperties()
        {
            Test1 test = new Test1() { Value = 1, ValueString = "test", Test = new Test2(), Tests = new List<Test2>() { new Test2(), new Test2() } };

            Assert.Equal(3, test.CopyProperties(new { Value = 3 }).Value);
            Assert.Equal(3, test.Value);

            Assert.Equal(4, test.CopyProperties(new { Value = 4 }, p => p.ValueString = "ciao").Value);
            Assert.Equal(4, test.Value);
            Assert.Equal("ciao", test.ValueString);

            Test1 test1 = new Test1();
            test1.CopyProperties(test.ExludeProperties(p => p.Value));
            Assert.Equal(0, test1.Value);
            Assert.Equal("ciao", test1.ValueString);
        }

        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "MergeWith")]
        public void MergeWith()
        {
            Test2 test = new Test2() { Value = 3, ValueString = "prova" };

            Dictionary<string, object> r1 = test.MergeWith(new { Value = 4, Prova = "lui" });

            Assert.Equal(new Dictionary<string, object>() { { "Value", 4 }, { "ValueString", "prova" }, { "Prova", "lui" } }, r1);
        }
    }
}
