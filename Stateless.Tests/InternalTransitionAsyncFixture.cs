using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Stateless.Tests
{
    public class InternalTransitionAsyncFixture
    {
        /// <summary>
        /// This unit test demonstrated bug report #417
        /// </summary>
        [UnityTest]
        public IEnumerator InternalTransitionAsyncIf_GuardExecutedOnlyOnce() => UniTask.ToCoroutine(async () =>
        {
            var guardCalls = 0;
            var order = new Order
            {
                Status = OrderStatus.OrderPlaced,
                PaymentStatus = PaymentStatus.Pending,
            };
            var stateMachine = new StateMachine<OrderStatus, OrderStateTrigger>(order.Status);
            stateMachine.Configure(OrderStatus.OrderPlaced)
                .InternalTransitionAsyncIf(OrderStateTrigger.PaymentCompleted,
                    () => PreCondition(ref guardCalls),
                    () => ChangePaymentState(order, PaymentStatus.Completed));

            await stateMachine.FireAsync(OrderStateTrigger.PaymentCompleted);

            Assert.AreEqual(1, guardCalls);
        });

        private bool PreCondition(ref int calls)
        {
            calls++;
            return true;
        }

        private async UniTask ChangePaymentState(Order order, PaymentStatus paymentStatus)
        {
            await UniTask.FromResult(order.PaymentStatus = paymentStatus);
        }

        private enum OrderStatus { OrderPlaced }
        private enum PaymentStatus { Pending, Completed }
        private enum OrderStateTrigger { PaymentCompleted }
        private class Order
        {
            public OrderStatus Status { get; internal set; }
            public PaymentStatus PaymentStatus { get; internal set; }
        }
    }
}
