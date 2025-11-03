// lib/models/invoice_model.dart (tạo mới nếu chưa có)
class InvoiceModel {
  final String invoiceId;
  final String receiverName;
  final String receiverPhone;
  final String receiverAddress;
  final int totalQuantity;
  final double totalPrice;
  final double discountAmount;
  final double totalPayment;
  final DateTime createdAt;
  final DateTime? canceledAt;
  final String paymentStatus;
  final String shippingStatus;
  final String paymentType;
  final String shippingMethod;
  final List<Map<String, dynamic>> productList;

  InvoiceModel({
    required this.invoiceId,
    required this.receiverName,
    required this.receiverPhone,
    required this.receiverAddress,
    required this.totalQuantity,
    required this.totalPrice,
    required this.discountAmount,
    required this.totalPayment,
    required this.createdAt,
    this.canceledAt,
    required this.paymentStatus,
    required this.shippingStatus,
    required this.paymentType,
    required this.shippingMethod,
    required this.productList,
  });

  factory InvoiceModel.fromJson(Map<String, dynamic> json) {
    return InvoiceModel(
      invoiceId: json['InvoiceID'],
      receiverName: json['ReceiverName'],
      receiverPhone: json['ReceiverPhone'],
      receiverAddress: json['ReceiverAddress'],
      totalQuantity: json['TotalQuantity'],
      totalPrice: json['TotalPrice'].toDouble(),
      discountAmount: json['DiscountAmount'].toDouble(),
      totalPayment: json['TotalPayment'].toDouble(),
      createdAt: DateTime.parse(json['CreatedAt']),
      canceledAt: json['CanceledAt'] != null
          ? DateTime.parse(json['CanceledAt'])
          : null,
      paymentStatus: json['PaymentStatus'],
      shippingStatus: json['ShippingStatus'],
      paymentType: json['PaymentType'],
      shippingMethod: json['ShippingMethod'],
      productList: List<Map<String, dynamic>>.from(json['ProductList']),
    );
  }
}
