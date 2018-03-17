using OMnG;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace UnitTest
{
    public class TypeExtensionsTests
    {
        #region nested type

        public interface IGeneric<T>
        {

        }
        public interface IInterfaceA
        {
            string PropA { get; set; }
            string ShadowPropA { get; }
        }
        public interface IInterfaceB { }
        public interface IInterfaceC : IInterfaceA, IInterfaceB { }

        public abstract class AbstractClass
        {
            public int PropAbstract { get; set; }
        }

        public class ClassA
        {
            public string PropA { get; set; }
        }
        public class ClassNotA
        {
            public int PropA { get; set; }
        }

        public class ClassB : AbstractClass, IInterfaceA
        {
            public string PropA { get; set; }
            public string ShadowPropA { get; }
        }

        public class ClassC : AbstractClass, IInterfaceC
        {
            public string PropA { get; set; }
            public string ShadowPropA { get; }
            public ClassA PropC { get; set; }
        }

        public class ClassD : ClassC, IGeneric<int>
        {
        }

        #endregion

        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(GetLablel))]
        public void GetLablel()
        {
            Assert.Equal("UnitTest.TypeExtensionsTests+ClassC", TypeExtensions.GetLabel<ClassC>());
        }

        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(GetLablels))]
        public void GetLablels()
        {
            Assert.Equal(new string[]
            {
                "UnitTest.TypeExtensionsTests+ClassC",
                "UnitTest.TypeExtensionsTests+IInterfaceC",
                "UnitTest.TypeExtensionsTests+IInterfaceA",
                "UnitTest.TypeExtensionsTests+IInterfaceB",
                "UnitTest.TypeExtensionsTests+AbstractClass"
            }, TypeExtensions.GetLabels<ClassC>());
        }

        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(GetLablelsCompress))]
        public void GetLablelsCompress()
        {
            TypeExtensions.Configuration = new TypeExtensionsConfiguration.CompressConfiguration();

            List<string> tmp = TypeExtensions.GetLabels<ClassC>().ToList();

            tmp.ForEach(p => Assert.True(p.Length <= 32));

            Assert.True(tmp.GetTypesFromLabels().GetInstanceOfMostSpecific() is ClassC);

            TypeExtensions.Configuration = new TypeExtensionsConfiguration.DefaultConfiguration();
        }

        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(GetTypesFromLabels))]
        public void GetTypesFromLabels()
        {
            Assert.Equal(new Type[]
            {
                typeof(ClassC),
                typeof(IInterfaceC),
                typeof(IInterfaceA),
                typeof(IInterfaceB),
                typeof(AbstractClass)
            }, TypeExtensions.GetLabels<ClassC>().GetTypesFromLabels());
        }
        
        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(GetInstanceOfMostSpecific))]
        public void GetInstanceOfMostSpecific()
        {
            var types = TypeExtensions.GetLabels<ClassC>().GetTypesFromLabels();

            Assert.True(types.GetInstanceOfMostSpecific() is ClassC);

            Assert.Null(types.Where(p => p != typeof(ClassC)).GetInstanceOfMostSpecific());
        }
        
        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(CheckObjectInclusion))]
        public void CheckObjectInclusion()
        {
            ClassC c = new ClassC();
            ClassA a = new ClassA();
            ClassNotA na = new ClassNotA();
            IInterfaceA ia = null;

            Assert.True(c.CheckObjectInclusion(a));
            Assert.False(c.CheckObjectInclusion(ia));
            Assert.False(c.CheckObjectInclusion(na));
        }

        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(GetLabels_Filter))]
        public void GetLabels_Filter()
        {
            Assert.Equal(new string[]
            {
                "UnitTest.TypeExtensionsTests+ClassD",
                "UnitTest.TypeExtensionsTests+IInterfaceC",
                "UnitTest.TypeExtensionsTests+IInterfaceA",
                "UnitTest.TypeExtensionsTests+IInterfaceB",
                "UnitTest.TypeExtensionsTests+ClassC",
                "UnitTest.TypeExtensionsTests+AbstractClass"
            }, TypeExtensions.GetLabels<ClassD>());
        }
    }
}
