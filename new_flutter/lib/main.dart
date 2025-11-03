import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'utils/api.dart';
import 'providers/user_provider.dart';
import 'screens/login_screen.dart';
import 'screens/register_screen.dart';
import 'screens/dashboard_screen.dart';
import 'screens/cart_screen.dart';
import 'screens/forgotpassword_screen.dart';
import 'screens/userpage_screen.dart';
import '../providers/cart_provider.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final prefs = await SharedPreferences.getInstance();
  final apiService = ApiService(prefs); // Truyền prefs trực tiếp
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(
          create: (_) => UserProvider(apiService: apiService),
        ),
        ChangeNotifierProvider(create: (_) => CartProvider()),
      ],
      child: MyApp(apiService: apiService),
    ),
  );
}

class MyApp extends StatelessWidget {
  final ApiService apiService;

  const MyApp({super.key, required this.apiService});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'MINCOW App',
      theme: ThemeData(
        primarySwatch:
            Colors.green, // Sử dụng màu chủ đạo #4CAF50 (gần với xanh lá)
        appBarTheme: const AppBarTheme(backgroundColor: Color(0xFF4CAF50)),
      ),
      initialRoute: '/login',
      routes: {
        '/login': (context) => const LoginScreen(),
        '/register': (context) => const RegisterScreen(),
        '/dashboard': (context) => const DashboardScreen(),
        '/cart': (context) => const CartScreen(),
        '/userpage': (context) => const UserpageScreen(),
        '/forgotpassword': (context) => const ForgotPasswordScreen(),
      },
      builder: FToastBuilder(),
    );
  }
}
