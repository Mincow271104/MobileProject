"Node.js version 22.14.0"
*Dòng dưới chỉ dùng nếu cần dùng nhiều version node, không thì install nodejs 22.14.0 là được
Xóa node.js (nếu có) -> Tải nvm -> Cài lại node.js đúng version [nvm install {version}] -> Dùng version đó [nvm use {version}]
-------------Hướng dẫn Setup Git-------------
tải git
chuột phải vào folder bên ngoài để chứa folder chứa code
chọn git bash
git config --global user.name "[tên]"
git config --global user.email "[email]"
git config --list (xem thông tin đã được lưu chưa)
*Mở vsc, chọn đúng folder chứa code sau đó mở powershell trong đó bằng ctrl + `và chạy 2 lệnh:
git clone -b be https://github.com/DoomXyz/ThatCook.git .
git clone -b fe https://github.com/DoomXyz/ThatCook.git .
-------------Hướng dẫn setup cơ bản-------------
*Làm 1 lần duy nhất:
git init
git remote add origin https://github.com/DoomXyz/That-Cook.git
*Đưa code lên
git add .
git commit -m '[tên gọi]'
git push origin [fe hoặc be]
*Kéo code xuống
git fetch origin [fe hoặc be]
git pull origin [fe hoặc be]
*Cài thư viện (Làm lại tương tự sau khi đã làm ở be/fe)
npm init
npm install
*Setup cổng
xóa .example ở file .env.example và thêm giá trị để dùng liên hệ để biết thêm chi tiết: https://www.facebook.com/starofthestarsofthestars/ 
-------------Hướng dẫn setup database-------------
vào myphpadmin nếu đã có database tên [thatcookdb] thì xóa đi
tạo lại database đặt tên là [thatcookdb]
mở ctrl +` chạy 2 lệnh dưới:
npx sequelize-cli db:migrate
npx sequelize-cli db:seed:all
*nếu chạy xong 2 lệnh hoặc bị lỗi khi đang chạy thì chạy lệnh dưới sau đó chạy lại 2 lệnh trên
npx sequelize-cli db:migrate:undo:all
---------------Danh sách mã lỗi trong code (errCode)-----------------
-1 = Tham số rỗng hoặc không hợp lệ (ví dụ: thiếu userInfo, accountname, password, accountid, token, userGender, userAccountType, page, limit).
0 = Xử lí yêu cầu thành công
1 = Định dạng sai (tên tài khoản, email, mật khẩu, số điện thoại, giới tính, loại tài khoản), trùng tên tài khoản/email/số điện thoại, mã không tồn tại (gender, accounttype), tham số filter/sort không hợp lệ, tạo mã tài khoản thất bại.
2 = Tài khoản không tồn tại, sai mật khẩu, tài khoản bị khóa, vượt quá số lần đăng nhập, token không hợp lệ, token hết hạn, token bị vô hiệu hóa.
3 = Lỗi cơ sở dữ liệu, lỗi mã hóa JWT, lỗi xác minh token, ngoại lệ không xử lý được, khóa bí mật JWT không được cấu hình.
----------------Set dữ liệu hình ảnh mẫu trong database mysql---------------------
*Chạy hết đoạn dưới vào SQL là được
UPDATE Banner
SET BannerImage = ELT(
FLOOR(1 + RAND() _ 3),
'https://res.cloudinary.com/dqblg6ont/image/upload/v1746186450/2_wa9qke.jpg',
'https://res.cloudinary.com/dqblg6ont/image/upload/v1746186450/defaultbanner_p9kvda.webp',
'https://res.cloudinary.com/dqblg6ont/image/upload/v1746186450/1_hto4q9.jpg'
);
UPDATE Product
SET ProductImage = ELT(
FLOOR(1 + RAND() _ 3),
'https://res.cloudinary.com/dqblg6ont/image/upload/v1744558124/byc7mrjoarwcgyrtea0r.jpg',
'https://res.cloudinary.com/dqblg6ont/image/upload/v1744558125/ls9e3xebscd1bmaneny8.jpg',
'https://res.cloudinary.com/dqblg6ont/image/upload/v1744558126/fsbxdr20pi2pjxebxrht.jpg'
);
UPDATE Image
SET Image = ELT(
FLOOR(1 + RAND() \* 3),
'https://res.cloudinary.com/dqblg6ont/image/upload/v1744558124/byc7mrjoarwcgyrtea0r.jpg',
'https://res.cloudinary.com/dqblg6ont/image/upload/v1744558125/ls9e3xebscd1bmaneny8.jpg',
'https://res.cloudinary.com/dqblg6ont/image/upload/v1744558126/fsbxdr20pi2pjxebxrht.jpg'
);

test
UPDATE Product
SET ProductImage = ELT(
FLOOR(1 + RAND() _ 10),
'https://cdn.hstatic.net/products/200000263355/z6871671614623_77962d7b1884c4dbb7760716e3baeab5_96ab5fd7278a4a74bb801e0709e30ce1_master.jpg',
'https://product.hstatic.net/200000263355/product/z5413482551660_0e5536590cc102da58115413ae1e272d_c1d09455225048dbb4f7f0cf42bdff58_master.jpg',
'https://cdn.hstatic.net/products/200000263355/z6763365968060_cfcc519f5a2bf8c6e1ded7d72c030816_ac528e4dc1cb45b397e2dd0c843439f1_master.jpg',
'https://cdn.hstatic.net/products/200000263355/z6763364252951_c901ebbd279887564807d56c8cf8f9af_c7c9a1635c7640ba8ee0ec74fc5de181_master.jpg',
'https://product.hstatic.net/200000263355/product/z6447619353649_2867001e3cc5b1e03f437bba1be79722_1daba2b4855c4694bece0ce1db9f55e7_master.jpg',
'https://product.hstatic.net/200000263355/product/z6447672845064_e78032c88cd748f9a177191cad5d8b57_8661f3af75224188b209e76f682fe184_master.jpg',
'https://product.hstatic.net/200000263355/product/z6447685581174_efff5c9cba2d8b30a0e359554ade0280_7e0087cfd38040939795fde3028d99ac_master.jpg',
'https://product.hstatic.net/200000263355/product/z6223606082117_577aa828dce515eaa0a93fc1feaf981e_8895d19971da426c80c5655279d07080_master.jpg',
'https://product.hstatic.net/200000263355/product/z6223723874413_07b654652f919a48c9dc75851eaecfdf_ee1165f0984742ae9d191cd591a86842_master.jpg',
'https://product.hstatic.net/200000263355/product/z6226529258524_a6f6ebbf81991b85fca6bdd78bd7c845_2f8e334b056c4e8e9a1954f16dffbaaa_master.jpg'
);
UPDATE Image
SET Image = ELT(
FLOOR(1 + RAND() _ 10),
'https://cdn.hstatic.net/products/200000263355/z6871671614623_77962d7b1884c4dbb7760716e3baeab5_96ab5fd7278a4a74bb801e0709e30ce1_master.jpg',
'https://product.hstatic.net/200000263355/product/z5413482551660_0e5536590cc102da58115413ae1e272d_c1d09455225048dbb4f7f0cf42bdff58_master.jpg',
'https://cdn.hstatic.net/products/200000263355/z6763365968060_cfcc519f5a2bf8c6e1ded7d72c030816_ac528e4dc1cb45b397e2dd0c843439f1_master.jpg',
'https://cdn.hstatic.net/products/200000263355/z6763364252951_c901ebbd279887564807d56c8cf8f9af_c7c9a1635c7640ba8ee0ec74fc5de181_master.jpg',
'https://product.hstatic.net/200000263355/product/z6447619353649_2867001e3cc5b1e03f437bba1be79722_1daba2b4855c4694bece0ce1db9f55e7_master.jpg',
'https://product.hstatic.net/200000263355/product/z6447672845064_e78032c88cd748f9a177191cad5d8b57_8661f3af75224188b209e76f682fe184_master.jpg',
'https://product.hstatic.net/200000263355/product/z6447685581174_efff5c9cba2d8b30a0e359554ade0280_7e0087cfd38040939795fde3028d99ac_master.jpg',
'https://product.hstatic.net/200000263355/product/z6223606082117_577aa828dce515eaa0a93fc1feaf981e_8895d19971da426c80c5655279d07080_master.jpg',
'https://product.hstatic.net/200000263355/product/z6223723874413_07b654652f919a48c9dc75851eaecfdf_ee1165f0984742ae9d191cd591a86842_master.jpg',
'https://product.hstatic.net/200000263355/product/z6226529258524_a6f6ebbf81991b85fca6bdd78bd7c845_2f8e334b056c4e8e9a1954f16dffbaaa_master.jpg'
);
