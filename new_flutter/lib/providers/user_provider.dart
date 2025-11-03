import 'dart:convert';
import 'package:flutter/material.dart';

import '../utils/api.dart';

class UserProvider with ChangeNotifier {
  final ApiService apiService;
  dynamic _user;

  UserProvider({required this.apiService});

  dynamic get user => _user;

  Future<Map<String, dynamic>> loginUser(
    String accountName,
    String password,
    bool rememberMe,
  ) async {
    final response = await apiService.login(
      accountName: accountName,
      password: password,
      rememberMe: rememberMe,
    );
    if (response['success']) {
      _user = response['user'];
      notifyListeners();
    }
    return response;
  }

  Future<void> logoutUser() async {
    await apiService.logout();
    _user = null;
    notifyListeners();
  }

  Future<void> verifyToken() async {
    final response = await apiService.verifyToken();
    if (response['success']) {
      _user = response['user'];
      notifyListeners();
    }
  }

  Future<Map<String, dynamic>> sendForgotToken(String email) async {
    return await apiService.sendForgotToken(email);
  }

  Future<Map<String, dynamic>> verifyForgotToken(
    String accountID,
    String verificationCode,
  ) async {
    return await apiService.verifyForgotToken(accountID, verificationCode);
  }

  Future<Map<String, dynamic>> changeForgotPassword(
    String accountID,
    String newPassword,
  ) async {
    return await apiService.changeForgotPassword(accountID, newPassword);
  }
}
