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

        public interface IValue
        {
            int Value { get; }
        }
        public class Test1 : IValue
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

        public class TestEveryType
        {
            public int Id { get; set; }
            public int Integer { get; set; }
            public int? IntegerNullable { get; set; }
            public double Double { get; set; }
            public DateTime DateTime { get; set; }
            public DateTime? DateTimeNullable { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public DateTimeOffset? DateTimeOffsetNullable { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public TimeSpan? TimeSpanNullable { get; set; }
            public string String { get; set; }

            public object Object { get; set; }
            public int ReadonlyInt { get; }
            public int WriteonlyInt { private get; set; }
        }

        private Dictionary<string, object> DefaultTestEntity = new Dictionary<string, object>()
        {
            { nameof(TestEveryType.Integer), 1},
            { nameof(TestEveryType.IntegerNullable), (int?)1},
            { nameof(TestEveryType.Double), 1.1},
            { nameof(TestEveryType.DateTime), new DateTime(1970, 1, 31)},
            { nameof(TestEveryType.DateTimeNullable), new DateTime(1970, 1, 31)},
            { nameof(TestEveryType.DateTimeOffset), (DateTimeOffset)new DateTime(1970, 1, 31)},
            { nameof(TestEveryType.DateTimeOffsetNullable), (DateTimeOffset)new DateTime(1970, 1, 31)},
            { nameof(TestEveryType.TimeSpan), new TimeSpan(1, 1, 1)},
            { nameof(TestEveryType.TimeSpanNullable), new TimeSpan(1, 1, 1)},
            { nameof(TestEveryType.String), "String"},
            { nameof(TestEveryType.Object), new object()}
        };

        private void PrepareEntity(TestEveryType entity)
        {
            entity.CopyProperties(DefaultTestEntity);

            entity.WriteonlyInt = 1;
        }

        private void CheckEquals(TestEveryType entity)
        {
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.Integer)], entity.Integer);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.IntegerNullable)], entity.IntegerNullable);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.Double)], entity.Double);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.DateTime)], entity.DateTime);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.DateTimeNullable)], entity.DateTimeNullable);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.DateTimeOffset)], entity.DateTimeOffset);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.DateTimeOffsetNullable)], entity.DateTimeOffsetNullable);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.TimeSpan)], entity.TimeSpan);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.TimeSpanNullable)], entity.TimeSpanNullable);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.String)], entity.String);
            Assert.Equal(DefaultTestEntity[nameof(TestEveryType.Object)], entity.Object);
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
            Assert.Equal(new string[] { "Tests" }, test.SelectMatchingTypesProperties(p=>p.IsCollection()).Keys);

            Assert.Equal(new string[] { "Value", "ValueString", "Test" }, test.ExludeMatchingTypesProperties(p=>p.IsCollection()).Keys);
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

            IValue value = new Test1().CopyProperties<IValue>(new { Value = 3 });
            Assert.Equal(3, value.Value);
        }
        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "CopyProperties2")]
        public void CopyProperties2()
        {
            TestEveryType tnode = new TestEveryType() { Id = 1 };
            PrepareEntity(tnode);

            using (ObjectExtensions.ConfigScope(new ObjectExtensionsConfiguration.DelegateILCachingConfiguration()))
            {
                CheckEquals(tnode);
            }
            using (ObjectExtensions.ConfigScope(new ObjectExtensionsConfiguration.DelegateCachingConfiguration()))
            {
                CheckEquals(tnode);
            }
        }

        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = "MergeWith")]
        public void MergeWith()
        {
            Test2 test = new Test2() { Value = 3, ValueString = "prova" };

            IDictionary<string, object> r1 = test.MergeWith(new { Value = 4, Prova = "lui" });

            Assert.Equal(new Dictionary<string, object>() { { "Value", 4 }, { "ValueString", "prova" }, { "Prova", "lui" } }, r1);
        }

        [Trait("Category", "ObjectExtensions")]
        [Fact(DisplayName = nameof(IsPrimitive))]
        public void IsPrimitive()
        {
            Test2 test = new Test2();

            Assert.True(3.IsPrimitive());
            Assert.False(test.IsPrimitive());
            Assert.True(typeof(int).IsPrimitive());
            Assert.False(typeof(Test2).IsPrimitive());
        }
    }
}
