class UserModel {
  final String accountId;
  final String accountName;
  final String userName;
  final String email;
  final String phone;
  final String address;
  final String gender;
  final String userImage;

  UserModel({
    required this.accountId,
    required this.accountName,
    required this.userName,
    required this.email,
    required this.phone,
    required this.address,
    required this.gender,
    required this.userImage,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      accountId: json['AccountID'] ?? '',
      accountName: json['AccountName'] ?? '',
      userName: json['UserName'] ?? '',
      email: json['Email'] ?? '',
      phone: json['Phone'] ?? '',
      address: json['Address'] ?? '',
      gender: json['Gender'] ?? '',
      userImage: json['UserImage'] ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'accountId': accountId,
      'accountName': accountName,
      'userName': userName,
      'email': email,
      'phone': phone,
      'address': address,
      'gender': gender,
      'userImage': userImage,
    };
  }
}
