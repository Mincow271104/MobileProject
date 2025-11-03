class BannerModel {
  final int bannerId;
  final String bannerImage;
  final String? productId;
  final String? productName;
  final String? productImage;

  BannerModel({
    required this.bannerId,
    required this.bannerImage,
    this.productId,
    this.productName,
    this.productImage,
  });

  factory BannerModel.fromJson(Map<String, dynamic> json) {
    return BannerModel(
      bannerId: json['BannerID'],
      bannerImage: json['BannerImage'],
      productId: json['ProductID'],
      productName: json['ProductName'],
      productImage: json['ProductImage'],
    );
  }
}
