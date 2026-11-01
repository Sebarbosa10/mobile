# Cómo publicar Hoop Rush en Google Play

Hoy el juego usa AdMob y Google Play Billing con **datos de prueba**. Estos pasos los hacés una sola vez
con tus cuentas; ningún dato real va en este repo salvo los IDs indicados.

## 1. Cuentas
- [ ] Crear la cuenta de Google Play Console (pago único de registro).
- [ ] Crear la cuenta de AdMob y vincularla a un método de cobro.
- [ ] Vincular el proyecto a Unity Cloud: **Edit › Project Settings › Services**. Unity IAP lo necesita.

## 2. Keystore
- [ ] En **Project Settings › Player › Android › Publishing Settings › Keystore Manager**, crear la keystore.
- [ ] Guardarla junto con sus contraseñas fuera del repo y con copia de seguridad. Sin ella no se pueden publicar
      actualizaciones firmadas con esa clave.

## 3. AdMob
- [ ] Crear la app Android en AdMob y dos bloques de anuncios: **Banner** e **Intersticial**.
- [ ] Copiar el App ID en **Assets › Google Mobile Ads › Settings**.
- [ ] Copiar los dos IDs de bloque en `AndroidBannerUnitId` y `AndroidInterstitialUnitId` de
      [MonetizationConfig.cs](Assets/Scripts/Monetization/MonetizationConfig.cs).
- [ ] En **Privacidad y mensajes**, crear y publicar el mensaje de GDPR. Sin esto, en Europa y Reino Unido
      no se muestran anuncios personalizados ni el formulario.
- [ ] Publicar `app-ads.txt` en un sitio propio, si AdMob lo pide.

## 4. Google Play Console
- [ ] Crear la app con el paquete `com.sebarbosa.hooprush`, o cambiarlo antes en
      [MobileBuildSettings.cs](Assets/Scripts/Editor/MobileBuildSettings.cs). No se puede cambiar después de publicar.
- [ ] Subir una primera versión a **Prueba interna**: Play no deja crear productos sin una build subida.
- [ ] Crear el producto único `remove_ads` a US$1. El ID tiene que coincidir con `RemoveAdsProductId`.
- [ ] Agregar tu cuenta en **Prueba de licencias** para comprar sin que te cobren.
- [ ] Completar **Seguridad de los datos**: AdMob recopila el ID de publicidad y datos del dispositivo.
- [ ] Declarar que la app contiene anuncios, la clasificación por edades y el público objetivo.
- [ ] Publicar una **política de privacidad** en una URL pública y cargarla en la ficha.
- [ ] Preparar la ficha en inglés: título `Hoop Rush: Street Basketball`, ícono de 512×512, gráfico de 1024×500, capturas y descripciones.
- [ ] Comprobar antes que el nombre esté libre en Google Play y que no choque con una marca registrada.

Si el público objetivo incluye menores de 13 años, avisá antes de publicar: AdMob y Play exigen otra
configuración de anuncios.

## 5. Build de publicación
- [ ] En iOS, reemplazar el identificador de la plantilla de Unity por uno propio antes de compilar para iPhone.
- [ ] Reemplazar la música, reactivarla en [AudioManager.cs](Assets/Scripts/Audio/AudioManager.cs) y volver a probar.
- [ ] Poner `ShowInReleaseBuilds` en `false` en [FpsCounter.cs](Assets/Scripts/Gameplay/FpsCounter.cs) para ocultar el contador de FPS.
- [ ] Subir `bundleVersion` y `Bundle Version Code` en cada versión.
- [ ] Generar **Tools › Build › Android AAB (publicación)**. Falla a propósito si siguen los IDs de prueba o no hay keystore.
- [ ] Subir `Builds/HoopRush.aab` a Prueba interna y comprobar anuncios, compra, reinstalación y recuperación de la compra.
