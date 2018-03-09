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

        #endregion

        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(GetLablel))]
        public void GetLablel()
        {
            Assert.Equal("UnitTest_TypeExtensionsTests__ClassC", TypeExtensions.GetLabel<ClassC>());
        }

        [Trait("Category", nameof(TypeExtensionsTests))]
        [Fact(DisplayName = nameof(GetLablels))]
        public void GetLablels()
        {
            Assert.Equal(new string[]
            {
                "UnitTest.TypeExtensionsTests+ClassC".EscapeName(),
                "UnitTest.TypeExtensionsTests+IInterfaceC".EscapeName(),
                "UnitTest.TypeExtensionsTests+IInterfaceA".EscapeName(),
                "UnitTest.TypeExtensionsTests+IInterfaceB".EscapeName(),
                "UnitTest.TypeExtensionsTests+AbstractClass".EscapeName()
            }, TypeExtensions.GetLabels<ClassC>());
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

            Assert.Throws<InvalidOperationException>(() => types.Where(p => p != typeof(ClassC)).GetInstanceOfMostSpecific());
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
    }
}
