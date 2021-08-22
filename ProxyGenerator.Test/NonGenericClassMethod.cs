using System;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;

namespace ProxyGenerator.Test
{
    public class NonGenericClassMethod
    {
        public interface INonGeneric
        {
            void Test();
            void Test(int i);
            void Test(int param1, int param2, int param3, int param4, int param5, int param6);
            decimal Test(string str);
            (decimal, string) Test(string str, decimal value);
        }
        [Test]
        public void VoidZeroParameterMethod()
        {
            var mock = new Moq.Mock<INonGeneric>();
            Type proxy = new ProxyMaker(typeof(INonGeneric)).CreateProxy();
            INonGeneric createdObjectFromProxy = Activator.CreateInstance(proxy, mock.Object) as INonGeneric;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test();
            mock.Verify(x=>x.Test());
        }
        [Test]
        public void VoidSingleParameterMethod()
        {
            var mock = new Moq.Mock<INonGeneric>();
            Type proxy = new Core.ProxyMaker(typeof(INonGeneric)).CreateProxy();
            INonGeneric createdObjectFromProxy = Activator.CreateInstance(proxy, mock.Object) as INonGeneric;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test(10);
            mock.Verify(x => x.Test(It.Is<int>(z=>z==10)));
        }
        [Test]
        public void VoidMoreThanThreeParameterMethod()
        {
            var mock = new Moq.Mock<INonGeneric>();
            Type proxy = new Core.ProxyMaker(typeof(INonGeneric)).CreateProxy();
            INonGeneric createdObjectFromProxy = Activator.CreateInstance(proxy, mock.Object) as INonGeneric;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test(1,2,3,4,5,6);
            mock.Verify(x => x.Test(It.IsIn(1), It.IsIn(2), It.IsIn(3), It.IsIn(4), It.IsIn(5), It.IsIn(6)));
        }
        [Test]
        public void DecimalReturnStringParameterMethod()
        {
            var mock = new Moq.Mock<INonGeneric>();
            const decimal expectedReturnValue = 12.907M;
            const string input = "Hello";
            mock.Setup(x => x.Test(It.Is<string>(z => z == input))).Returns(expectedReturnValue);
            Type proxy = new Core.ProxyMaker(typeof(INonGeneric)).CreateProxy();
            INonGeneric createdObjectFromProxy = Activator.CreateInstance(proxy, mock.Object) as INonGeneric;
            Assert.NotNull(createdObjectFromProxy);
            decimal actualRv = createdObjectFromProxy.Test(input);
            Assert.AreEqual(expectedReturnValue,actualRv);
        }
        [Test]
        public void TupleReturnStringParameterMethod()
        {
            var mock = new Moq.Mock<INonGeneric>();

            const string expectedStringRv = "Hello";
            const decimal expectedDecimalRv = 123.978M;
            //Return the value in reverse order
            mock.Setup(x => x.Test(It.Is<string>(z => z == expectedStringRv),
                    It.Is<decimal>(z => z == expectedDecimalRv)))
                .Returns((expectedDecimalRv, expectedStringRv));
            Type proxy = new Core.ProxyMaker(typeof(INonGeneric)).CreateProxy();
            INonGeneric createdObjectFromProxy = Activator.CreateInstance(proxy, mock.Object) as INonGeneric;
            Assert.NotNull(createdObjectFromProxy);
            var (actualDecimalRv,actualStringRv) = createdObjectFromProxy.Test(expectedStringRv, expectedDecimalRv);
            Assert.AreEqual(expectedDecimalRv, actualDecimalRv);
            Assert.AreEqual(expectedStringRv, actualStringRv);
        }

    }
}