from PIL import Image, ImageDraw, ImageFilter
import math
import os
import time

def create_hero_art(
    img1_path, 
    img2_path, 
    img3_path, 
    out_path, 
    canvas_size=(1920, 1080), 
    dark_bg_color=(15, 23, 42)
):
    t0 = time.time()
    W, H = canvas_size
    scale = W / 1920.0
    
    mx, my = int(60 * scale), int(108 * scale)
    banner_w = W - 2 * mx
    banner_h = H - 2 * my

    # 1. Base Canvas
    canvas = Image.new('RGBA', (W, H), dark_bg_color + (255,))
    
    # 2. Fast Gradient Banner Background
    # Generate small gradient and upscale with LANCZOS for high quality and speed
    grad_w, grad_h = 160, 90
    small_grad = Image.new('RGB', (grad_w, grad_h))
    for y in range(grad_h):
        ty = y / float(grad_h)
        for x in range(grad_w):
            tx = x / float(grad_w)
            
            # Base light mint gradient tones
            r = int((1.0 - tx)*219 + tx*229)
            g = int((1.0 - ty)*236 + ty*242)
            b = int((1.0 - tx)*224 + tx*233)
            
            # Radial highlight around top-left center (x=0.35, y=0.35)
            dist = math.sqrt((tx - 0.35)**2 + (ty - 0.35)**2)
            glow = max(0.0, 1.0 - dist / 0.6)
            r = min(255, int(r + glow * 18))
            g = min(255, int(g + glow * 16))
            b = min(255, int(b + glow * 18))
            
            small_grad.putpixel((x, y), (r, g, b))
            
    banner = small_grad.resize((banner_w, banner_h), Image.Resampling.LANCZOS).convert('RGBA')
            
    # Rounded corner mask for banner
    banner_mask = Image.new('L', (banner_w, banner_h), 0)
    draw_bm = ImageDraw.Draw(banner_mask)
    banner_radius = int(24 * scale)
    draw_bm.rounded_rectangle([0, 0, banner_w, banner_h], radius=banner_radius, fill=255)
    
    canvas.paste(banner, (mx, my), mask=banner_mask)
    
    # 3. Load & Scale Screenshots
    im1 = Image.open(img1_path).convert('RGBA')
    im2 = Image.open(img2_path).convert('RGBA')
    im3 = Image.open(img3_path).convert('RGBA')
    
    h1 = int(710 * scale)
    w1 = int(im1.width * (h1 / float(im1.height)))
    im1_scaled = im1.resize((w1, h1), Image.Resampling.LANCZOS)
    
    h2 = int(680 * scale)
    w2 = int(im2.width * (h2 / float(im2.height)))
    im2_scaled = im2.resize((w2, h2), Image.Resampling.LANCZOS)
    
    h3 = int(630 * scale)
    w3 = int(im3.width * (h3 / float(im3.height)))
    im3_scaled = im3.resize((w3, h3), Image.Resampling.LANCZOS)
    
    cy = my + banner_h // 2
    
    x1, y1 = int(250 * scale), cy - h1 // 2
    x2, y2 = int(725 * scale), cy - h2 // 2
    x3, y3 = int(1215 * scale), cy - h3 // 2
    
    cards = [
        (im1_scaled, x1, y1, w1, h1),
        (im2_scaled, x2, y2, w2, h2),
        (im3_scaled, x3, y3, w3, h3),
    ]
    
    # Card & Drop Shadow Helper
    def add_card_with_shadow(parent_canvas, img, x, y):
        w, h = img.size
        radius = int(14 * scale)
        shadow_blur = int(22 * scale)
        shadow_dy = int(12 * scale)
        
        pad = shadow_blur * 2
        shadow_w, shadow_h = w + 2*pad, h + 2*pad
        shadow_img = Image.new('RGBA', (shadow_w, shadow_h), (0, 0, 0, 0))
        draw_s = ImageDraw.Draw(shadow_img)
        draw_s.rounded_rectangle(
            [pad, pad, pad + w, pad + h], 
            radius=radius, 
            fill=(10, 20, 35, 110)
        )
        shadow_blurred = shadow_img.filter(ImageFilter.GaussianBlur(shadow_blur))
        
        parent_canvas.paste(
            shadow_blurred, 
            (x - pad, y - pad + shadow_dy), 
            mask=shadow_blurred
        )
        
        card_mask = Image.new('L', (w, h), 0)
        draw_cm = ImageDraw.Draw(card_mask)
        draw_cm.rounded_rectangle([0, 0, w, h], radius=radius, fill=255)
        
        card_border = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        draw_cb = ImageDraw.Draw(card_border)
        border_width = max(1, int(2 * scale))
        draw_cb.rounded_rectangle(
            [0, 0, w-1, h-1], 
            radius=radius, 
            outline=(30, 41, 59, 220), 
            width=border_width
        )
        
        card_composite = Image.alpha_composite(img, card_border)
        parent_canvas.paste(card_composite, (x, y), mask=card_mask)

    for img_s, x, y, w, h in cards:
        add_card_with_shadow(canvas, img_s, x, y)
        
    final_rgb = Image.new('RGB', (W, H), dark_bg_color)
    final_rgb.paste(canvas, (0, 0), mask=canvas)
    
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    final_rgb.save(out_path, 'PNG', optimize=True)
    dt = time.time() - t0
    print(f'Generated: {out_path} ({W}x{H}) in {dt:.2f}s')

def build_all():
    img1 = 'screenshots/new/2026-08-01_17-06_1.png'
    img2 = 'screenshots/new/2026-08-01_17-08.png'
    img3 = 'screenshots/new/2026-08-01_17-06.png'
    
    targets = [
        ('store_assets/hero_1920x1080.png', (1920, 1080)),
        ('store_assets/hero_3840x2160.png', (3840, 2160)),
        ('store_assets/screenshot_1_cover_1920x1080.png', (1920, 1080)),
        ('screenshots/cover.png', (1920, 1080)),
        ('screenshots/new/hero_1920x1080.png', (1920, 1080)),
        ('screenshots/new/hero_3840x2160.png', (3840, 2160)),
    ]
    
    for path, size in targets:
        create_hero_art(img1, img2, img3, path, canvas_size=size)

if __name__ == '__main__':
    build_all()
