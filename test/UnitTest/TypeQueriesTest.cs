using OMnG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace UnitTest
{
    public class TypeQueriesTest
    {
        #region nested types

        public interface Interface1
        {

        }
        public interface Interface2
        {

        }
        public interface InterfaceAll : Interface1, Interface2
        {

        }

        public class BaseClass : Interface1
        {

        }

        public abstract class AbstractClass : BaseClass
        {

        }

        public class SubClass1 : AbstractClass
        {

        }
        public class SubClass2 : AbstractClass, Interface2
        {

        }
        public class SubSubClass1 : SubClass1, InterfaceAll, ICollection<SubClass1>, ICollection<InterfaceAll>
        {
            public int Count => 0;

            public bool IsReadOnly => false;

            public void Add(SubClass1 item)
            {
                throw new NotImplementedException();
            }

            public void Add(InterfaceAll item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(SubClass1 item)
            {
                throw new NotImplementedException();
            }

            public bool Contains(InterfaceAll item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(SubClass1[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(InterfaceAll[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<SubClass1> GetEnumerator()
            {
                return new List<SubClass1>().GetEnumerator();
            }

            public bool Remove(SubClass1 item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(InterfaceAll item)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new List<SubClass1>().GetEnumerator();
            }

            IEnumerator<InterfaceAll> IEnumerable<InterfaceAll>.GetEnumerator()
            {
                return new List<InterfaceAll>().GetEnumerator();
            }
        }

        #endregion

        [Trait("Category", nameof(TypeQueriesTest))]
        [Fact(DisplayName = nameof(TypeHolderOperators))]
        public void TypeHolderOperators()
        {
            TypeHolder holder = typeof(SubSubClass1);
            Assert.True(holder == typeof(SubSubClass1));
            Assert.True(holder != typeof(SubClass1));

            Assert.True(holder <= typeof(SubSubClass1));
            Assert.True(holder >= typeof(SubSubClass1));
            Assert.True(typeof(SubSubClass1)<=holder);
            Assert.True(typeof(SubClass1) < holder);
            Assert.True(holder>typeof(SubClass1));

            holder = typeof(Interface2);

            Assert.False(holder < typeof(AbstractClass));
            Assert.False(holder <= typeof(AbstractClass));
            Assert.False(holder > typeof(AbstractClass));
            Assert.False(holder >= typeof(AbstractClass));
            Assert.False(holder == typeof(AbstractClass));
            Assert.True(holder != typeof(AbstractClass));
        }

        [Trait("Category", nameof(TypeQueriesTest))]
        [Fact(DisplayName = nameof(TypeHolderQueries))]
        public void TypeHolderQueries()
        {
            Assert.True(typeof(SubSubClass1).IsCollection());
            Assert.True(typeof(SubSubClass1).IsEnumerable());
            Assert.True(typeof(SubSubClass1).IsEnumerableOfAssignableTypes(typeof(SubClass1)));
            Assert.True(typeof(SubSubClass1).IsCollectionOfAssignableTypes(typeof(SubClass1)));
            Assert.True(typeof(SubSubClass1).IsEnumerableOfAssignableTypes(typeof(InterfaceAll)));
            Assert.True(typeof(SubSubClass1).IsCollectionOfAssignableTypes(typeof(InterfaceAll)));
            Assert.True(typeof(SubSubClass1).IsEnumerableOfAssignableTypes(typeof(Interface1)));
            Assert.True(typeof(SubSubClass1).IsCollectionOfAssignableTypes(typeof(Interface1)));
            Assert.True(typeof(SubSubClass1).IsEnumerableOfAssignableTypes(typeof(Interface2)));
            Assert.True(typeof(SubSubClass1).IsCollectionOfAssignableTypes(typeof(Interface2)));
            Assert.False(typeof(SubSubClass1).IsEnumerableOfAssignableTypes(typeof(SubClass2)));
            Assert.False(typeof(SubSubClass1).IsCollectionOfAssignableTypes(typeof(SubClass2)));

            Assert.True(typeof(SubClass1).IsConvertibleTo(typeof(SubClass1)));
            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(SubClass1)));
            Assert.False(typeof(SubClass1).IsConvertibleTo(typeof(SubSubClass1)));

            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(IEnumerable<SubClass1>)));
            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(ICollection<SubClass1>)));
            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(IEnumerable<InterfaceAll>)));
            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(ICollection<InterfaceAll>)));
            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(IEnumerable<Interface1>)));
            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(ICollection<Interface1>)));
            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(IEnumerable<Interface2>)));
            Assert.True(typeof(SubSubClass1).IsConvertibleTo(typeof(ICollection<Interface2>)));
            Assert.False(typeof(SubSubClass1).IsConvertibleTo(typeof(IEnumerable<SubClass2>)));
            Assert.False(typeof(SubSubClass1).IsConvertibleTo(typeof(ICollection<SubClass2>)));

            Assert.True(typeof(IEnumerable<InterfaceAll>).IsEnumerableOfAssignableTypes(typeof(InterfaceAll)));
            Assert.True(typeof(ICollection<InterfaceAll>).IsCollectionOfAssignableTypes(typeof(InterfaceAll)));

            Assert.Equal(
                new List<TypeHolder[]>() { new TypeHolder[] { typeof(string), typeof(SubClass1) } },
                typeof(Dictionary<string, SubClass1>).GetGenericArgumentsOf(typeof(IDictionary<,>)));

            Assert.True(typeof(Dictionary<string, SubClass1>).IsOfGenericType(typeof(IDictionary<,>)));
            Assert.True(typeof(Dictionary<string, SubClass1>).IsOfGenericType(typeof(IDictionary<,>),p=>p == typeof(string)));
            Assert.True(typeof(Dictionary<string, SubClass1>).IsOfGenericType(typeof(IDictionary<,>), p => p == typeof(string), p=>p > typeof(Interface1)));
            Assert.False(typeof(Dictionary<string, SubClass1>).IsOfGenericType(typeof(IDictionary<,>), p => p == typeof(string),p => p > typeof(Interface2)));

            Assert.Equal(typeof(AbstractClass), typeof(SubSubClass1).AsTypeEnumerable().First(p=>p.Type.IsAbstract && !p.Type.IsInterface));
            Assert.Equal(1, typeof(SubSubClass1).AsTypeEnumerable().Count(p => p.Type.IsAbstract && !p.Type.IsInterface));

            Assert.True(typeof(int).IsConvertibleTo(typeof(double)));
        }

        [Trait("Category", nameof(TypeQueriesTest))]
        [Fact(DisplayName = nameof(TypeHolderConvertTo))]
        public void TypeHolderConvertTo()
        {
            object nothing = null;

            Assert.Equal(2, 2.2.ConvertTo<int>());
            Assert.Equal(0, TestEnum.A.ConvertTo<int>());
            Assert.Equal(TestEnum.C, 2.ConvertTo<TestEnum>());
            Assert.Equal(default(float), nothing.ConvertTo<float>());

            Assert.True(typeof(int).IsConvertibleTo(typeof(int?)));
            Assert.Equal(2, ((double?)2.2).ConvertTo<int>());
            Assert.True(typeof(int?).IsConvertibleTo(typeof(int)));
            Assert.Equal(2, (2.2).ConvertTo<int?>());

            List<SubSubClass1> values = new List<SubSubClass1>() { new SubSubClass1(), new SubSubClass1() };
            List<SubClass1> newValues = ((IEnumerable<SubClass1>)values.ConvertTo<IEnumerable<SubClass1>>()).ToList();
            Assert.Equal(values[0], newValues[0]);
            Assert.Equal(values[1], newValues[1]);

            Dictionary<int, SubSubClass1> dict = new Dictionary<int, SubSubClass1>() { { 0, new SubSubClass1() }, { 1, new SubSubClass1() } };
            Dictionary<string, SubClass1> newDict = (Dictionary<string, SubClass1>)dict.ConvertTo<Dictionary<string, SubClass1>>();
            Assert.Equal(dict[0], newDict["0"]);
            Assert.Equal(dict[1], newDict["1"]);

            Dictionary<string, SubClass1> newDict2 = ((IEnumerable<KeyValuePair<string, SubClass1>>)dict
                .ConvertTo<IEnumerable<KeyValuePair<string, SubClass1>>>())
                .ToDictionary(p=>p.Key, p=>p.Value);
            Assert.Equal(dict[0], newDict2["0"]);
            Assert.Equal(dict[1], newDict2["1"]);

            Dictionary<string, SubClass1> newDict3 = ((IDictionary<string, SubClass1>)dict
                .ConvertTo<IDictionary<string, SubClass1>>())
                .ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(dict[0], newDict3["0"]);
            Assert.Equal(dict[1], newDict3["1"]);
        }
    }
}
