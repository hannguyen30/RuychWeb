﻿namespace RuychWeb.Models.Vnpay
{
    public class PaymentInformationModel
    {
        public string OrderType { get; set; }
        public decimal Amount { get; set; }
        public string OrderDescription { get; set; }
        public string Name { get; set; }
        public int OrderId { get; set; }
    }
}
