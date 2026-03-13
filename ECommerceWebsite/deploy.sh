#!/bin/bash

# سكريبت النشر التلقائي لموقع التجارة الإلكترونية
# استخدم هذا السكريبت لحل مشاكل النشر بسرعة

echo "🚀 بدء عملية النشر..."

# 1. إنشاء مجلد الصور إذا لم يكن موجوداً
echo "📁 إنشاء مجلد الصور..."
mkdir -p wwwroot/images/products

# 2. تنزيل صورة افتراضية للمنتجات
echo "🖼️ تنزيل صورة افتراضية للمنتجات..."
curl -o wwwroot/images/products/phone1.jpg "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400&h=400&fit=crop"

# 3. بناء المشروع
echo "🔨 بناء المشروع..."
dotnet build --configuration Release

# 4. نشر المشروع
echo "📦 نشر المشروع..."
dotnet publish --configuration Release --output ./publish

# 5. نسخ الملفات المهمة للنشر
echo "📋 نسخ الملفات المطلوبة..."
cp SampleData.sql publish/
cp appsettings.Production.json publish/
cp .env publish/

echo "✅ تم إعداد المشروع للنشر!"
echo ""
echo "📋 الملفات الجاهزة للنشر:"
echo "  - publish/ (مجلد النشر الكامل)"
echo "  - SampleData.sql (بيانات تجريبية)"
echo "  - appsettings.Production.json (إعدادات الإنتاج)"
echo "  - .env (متغيرات البيئة)"
echo ""
echo "🔧 في لوحة الاستضافة:"
echo "  1. ارفع محتويات مجلد publish/"
echo "  2. أضف متغير البيئة: ASPNETCORE_ENVIRONMENT=Development"
echo "  3. شغل ملف SampleData.sql في قاعدة البيانات"
echo "  4. اختبر صفحة المنتجات"
echo ""
echo "🎉 النشر جاهز!"
