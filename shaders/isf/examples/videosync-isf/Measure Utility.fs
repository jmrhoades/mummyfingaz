/*
{
  "ISFVSN": "2",
  "CATEGORIES": ["Utility"],
  "CREDIT": "Tarik Barri",
  "DESCRIPTION": "Display RGBA values, coordinates, and transport time. Digit display adapted from VIDVOX 'Digital Clock'",
  "PASSES": [{}],
  "INPUTS": [
    {"NAME": "inputImage", "TYPE": "image"},
    {
      "NAME": "measurePoint", "LABEL": "Measure Point (in UV Coordinates)",
      "TYPE": "point2D", "DEFAULT": [0.5, 0.5], "MIN": [0, 0], "MAX": [1, 1]
    },
    {"NAME": "showCoordinates", "LABEL": "Show Coordinates", "TYPE": "bool", "DEFAULT": 1.0},
    {"NAME": "showColor", "LABEL": "Show Color Values", "TYPE": "bool", "DEFAULT": 1.0},
    {"NAME": "showBeat", "LABEL": "Show Arrangement Position", "TYPE": "bool", "DEFAULT": 1.0},
    {"NAME": "textColor", "LABEL": "Text Color", "TYPE": "color", "DEFAULT": [1.0, 1.0, 1.0, 1.0]},
    {
      "NAME": "coordMode", "LABEL": "Coordinate Mode", "TYPE": "long", "DEFAULT": 1,
      "VALUES": [0, 1, 2, 3],
      "LABELS": [
        "Screen Space - Absolute Coordinates",
        "UV/isf_FragNormCoord coordinates",
        "NDC - Normalized Device Coordinates",
        "Equidistant Normalized (Videosync Default)"
      ]
    }
  ]
}
*/

// Display configuration
const float DIGIT_SPACING = 1.5;
const float DEFAULT_SCALE = 0.026;

// Numeric constants
const float EPSILON = 0.000001;
const float MAX_FLOAT_VALUE = 99999.0;
const float POWERS_OF_10[6] = float[6](1.0, 10.0, 100.0, 1000.0, 10000.0, 100000.0);

// Segment positions (for 7-segment display)
const vec3 TOP         = vec3(-1.0,  0.0, 1);  // horizontal, top
const vec3 MIDDLE      = vec3( 0.0,  0.0, 1);  // horizontal, middle
const vec3 BOTTOM      = vec3( 1.0,  0.0, 1);  // horizontal, bottom
const vec3 TOPLEFT     = vec3( 0.5, -0.5, 0);  // vertical, top left
const vec3 TOPRIGHT    = vec3(-0.5, -0.5, 0);  // vertical, top right
const vec3 BOTTOMLEFT  = vec3( 0.5,  0.5, 0);  // vertical, bottom left
const vec3 BOTTOMRIGHT = vec3(-0.5,  0.5, 0);  // vertical, bottom right


//========================================
// Coordinate System Conversions
//========================================

// Keep relative coordinates in tact regardless of screen ratio

vec2 xyRatio = RENDERSIZE.x > RENDERSIZE.y ? vec2(1.0, RENDERSIZE.y / RENDERSIZE.x) : vec2(RENDERSIZE.x / RENDERSIZE.y, 1.0);

vec2 equidistCoordToNormCoord(vec2 equidistCoord) {
    vec2 aspectRatioOffset = (max(RENDERSIZE.x, RENDERSIZE.y) - RENDERSIZE) * 0.5;
    return (equidistCoord + 1.0) * (RENDERSIZE + 2.0 * aspectRatioOffset) / (2.0 * RENDERSIZE) - aspectRatioOffset / RENDERSIZE;
}

vec2 normCoordToEquidistCoord(vec2 normCoord) {
    vec2 aspectRatioOffset = (max(RENDERSIZE.x, RENDERSIZE.y) - RENDERSIZE) * 0.5;
    return (2.0 * RENDERSIZE * normCoord + 2.0 * aspectRatioOffset) / (RENDERSIZE + 2.0 * aspectRatioOffset) - 1.0;
}

//========================================
// Segment Drawing Functions
//========================================
vec4 segment(vec2 uv, bool vertical, vec4 color, bool isOutline) {
    if (!vertical) {
        uv = uv.yx;
    }
    
    float segmentWidth = 0.35;      // Controls basic thickness of the segment
    float lengthRatio = 0.42;       // Controls length of the segment
    float outlineWidth = 0.5;       // Controls thickness of outline
    float glowPower = 3.0;          // Controls edge falloff (higher = sharper)
    float outlineOpacityMult = 1.5;
    
    // Calculate segment boundaries
    float innerStart = segmentWidth * 0.1;     // Start of inner edge
    float innerEnd = segmentWidth;             // End of inner edge
    float outerStart = lengthRatio;            // Start of length cutoff
    float outerEnd = lengthRatio * 1.57;       // End of length cutoff

    float xDist = abs(uv.x);
    float yDist = abs(uv.y);
    
    float opacity;
    if (isOutline) {
        // Outline segment (black border)
        opacity = (1.0 - smoothstep(innerStart - outlineWidth, 
                                  innerEnd + outlineWidth, xDist)) * 
                 (1.0 - smoothstep(outerStart - outlineWidth, 
                                  outerEnd + outlineWidth, yDist + xDist));
        return vec4(0.0, 0.0, 0.0, opacity * outlineOpacityMult);
    } else {
        // Colored segment
        opacity = (1.0 - smoothstep(innerStart, innerEnd, xDist)) * 
                 (1.0 - smoothstep(outerStart, outerEnd, yDist + xDist));
        return color * pow(opacity, glowPower);
    }
}

vec4 drawMinus(vec2 uv, vec4 color) {
    return segment(uv.yx, true, color, true) + segment(uv.yx, true, color, false);
}

vec4 drawDot(vec2 uv, vec2 cCenter, float cRadius, vec4 color) {
    float dist = distance(uv, cCenter);
    float outlineWidth = 0.22;  // Adjust this to control outline thickness
    
    // Draw black outline
    float outlineOpacity = dist > (cRadius + outlineWidth) ? 0.0 : 1.0 - pow(dist / (cRadius + outlineWidth), 4.0);
    vec4 outline = vec4(0.0, 0.0, 0.0, outlineOpacity * 0.6);
    
    // Draw colored dot
    float dotOpacity = dist > cRadius ? 0.0 : 1.0 - pow(dist / cRadius, 4.0);
    vec4 dot = color * dotOpacity;
    
    return outline + dot;
}

vec4 drawSegment(vec2 uv, vec3 segPos, vec4 color, bool isOutline) {
    vec2 offset = segPos.xy;
    bool isHorizontal = segPos.z > 0.5;
    if (isHorizontal) {
        uv = uv.yx;
    }
    return segment(uv + offset, true, color, isOutline);
}

// Segment patterns for each digit/character
//  ┌─0─┐
//  3   4
//  ├─1─┤
//  5   6
//  └─2─┘
// Order: TOP(0), MIDDLE(1), BOTTOM(2), TOPLEFT(3), TOPRIGHT(4), BOTTOMLEFT(5), BOTTOMRIGHT(6)
const int[7] SEGMENTS_0 = int[7](1, 0, 1, 1, 1, 1, 1);  // 0
const int[7] SEGMENTS_1 = int[7](0, 0, 0, 0, 1, 0, 1);  // 1
const int[7] SEGMENTS_2 = int[7](1, 1, 1, 0, 1, 1, 0);  // 2
const int[7] SEGMENTS_3 = int[7](1, 1, 1, 0, 1, 0, 1);  // 3
const int[7] SEGMENTS_4 = int[7](0, 1, 0, 1, 1, 0, 1);  // 4
const int[7] SEGMENTS_5 = int[7](1, 1, 1, 1, 0, 0, 1);  // 5
const int[7] SEGMENTS_6 = int[7](1, 1, 1, 1, 0, 1, 1);  // 6
const int[7] SEGMENTS_7 = int[7](1, 0, 0, 0, 1, 0, 1);  // 7
const int[7] SEGMENTS_8 = int[7](1, 1, 1, 1, 1, 1, 1);  // 8
const int[7] SEGMENTS_9 = int[7](1, 1, 1, 1, 1, 0, 1);  // 9
const int[7] SEGMENTS_N = int[7](1, 0, 0, 1, 1, 1, 1);  // N
const int[7] SEGMENTS_A = int[7](1, 1, 0, 1, 1, 1, 1);  // A
const int[7] SEGMENTS_F = int[7](1, 1, 0, 1, 0, 1, 0);  // F

int[7] getDigitPattern(int num) {
    if (num == 0) return SEGMENTS_0;
    else if (num == 1) return SEGMENTS_1;
    else if (num == 2) return SEGMENTS_2;
    else if (num == 3) return SEGMENTS_3;
    else if (num == 4) return SEGMENTS_4;
    else if (num == 5) return SEGMENTS_5;
    else if (num == 6) return SEGMENTS_6;
    else if (num == 7) return SEGMENTS_7;
    else if (num == 8) return SEGMENTS_8;
    else if (num == 9) return SEGMENTS_9;
    else if (num == -1) return SEGMENTS_N;
    else if (num == -2) return SEGMENTS_A;
    else if (num == -3) return SEGMENTS_F;
    return SEGMENTS_0;
}

vec4 drawSingleDigit(vec2 uv, int num, vec4 color) {
    vec4 result = vec4(0.0);
    int[7] segments = getDigitPattern(num);
    
    // Draw outline and segments in two passes
    for (int pass = 0; pass < 2; pass++) {
        bool isOutline = (pass == 0);
        vec3 positions[7] = vec3[7](TOP, MIDDLE, BOTTOM, TOPLEFT, TOPRIGHT, BOTTOMLEFT, BOTTOMRIGHT);
        
        for (int i = 0; i < 7; i++) {
            if (segments[i] == 1) {
                result += drawSegment(uv, positions[i], color, isOutline);
            }
        }
    }
    return result;
}

vec4 drawNaN(vec2 uv, float scale, vec2 offset, vec4 color) {
    vec4 result = vec4(0.0);
    vec2 stepSize = vec2(DIGIT_SPACING, 0.0);
    uv = uv/scale;
    result += drawSingleDigit(uv - (offset - stepSize * 3), -1, color); // N
    result += drawSingleDigit(uv - (offset - stepSize * 2), -2, color); // A
    result += drawSingleDigit(uv - (offset - stepSize * 1), -1, color); // N
    return result;
}

vec4 drawInf(vec2 uv, float scale, vec2 offset, vec4 color, bool positive) {
    vec4 result = vec4(0.0);
    vec2 stepSize = vec2(DIGIT_SPACING, 0.0);
    uv = uv/scale;
    if (!positive) {
        result += drawMinus(uv - (offset - stepSize * 4), color);
    }
    result += drawSingleDigit(uv - (offset - stepSize * 3), 1, color);  // I
    result += drawSingleDigit(uv - (offset - stepSize * 2), -1, color); // N
    result += drawSingleDigit(uv - (offset - stepSize * 1), -3, color); // F
    return result;
}

bool isPosInf(float val) {
    return val == (1.0/0.0);
}

bool isNegInf(float val) {
    return val == (-1.0/0.0);
}

bool isNaN(float val) {
    return !(val <= 0.0 || 0.0 <= val);  // NaN is the only value that returns false for both comparisons
}

vec4 getFloatDigitsColor(vec2 uv, float scale, vec2 offset, float floatNum, int integerDigits, int decimalPlaces, vec4 color) {
    if (isNaN(floatNum)) {
        return drawNaN(uv, scale, offset, color);
    } else if (isPosInf(floatNum)) {
        return drawInf(uv, scale, offset, color, true);
    } else if (isNegInf(floatNum)) {
        return drawInf(uv, scale, offset, color, false);
    }
    
    if (abs(floatNum) < EPSILON) {
        floatNum = 0.0;
    } else {
        floatNum += floatNum > 0.0 ? EPSILON : -EPSILON;
    }
    
    int actualIntDigits = 1;
    int tempInt = int(abs(floatNum));
    for(int i = 0; i < 6; i++) {
        if (tempInt < 10) break;
        tempInt /= 10;
        actualIntDigits++;
    }
    
    integerDigits = max(max(1, integerDigits), actualIntDigits);
    decimalPlaces = max(0, decimalPlaces);
    
    float totalWidth = float(integerDigits + (decimalPlaces > 0 ? 1 : 0) + decimalPlaces) * DIGIT_SPACING;
    offset.x -= totalWidth;
    
    uv = uv/scale;
    int totalDigits = integerDigits + decimalPlaces;
    vec4 digitColor = vec4(0.0);
    vec2 stepSize = vec2(DIGIT_SPACING, 0.0);
    
    bool doDrawMinus = floatNum < 0.0;
    if (doDrawMinus) {
        floatNum = -floatNum;
        digitColor += drawMinus(uv - (offset - stepSize), color);
    }
    
    int intPart = int(floatNum);
    float fracPart = fract(floatNum);
    
    for (int stepAmt = 0; stepAmt < totalDigits; stepAmt++) {
        if (stepAmt < integerDigits) {
            int place = integerDigits - stepAmt - 1;
            int div = int(POWERS_OF_10[min(place, 5)]);
            int displayNum = (intPart / div) % 10;
            
            digitColor += drawSingleDigit(uv - (offset + stepSize * float(stepAmt)), displayNum, color);
            
            if (stepAmt == integerDigits - 1 && decimalPlaces > 0) {
                digitColor += drawDot(uv - (offset + stepSize * float(stepAmt + 1)), vec2(0, -1), 0.2, color);
            }
        } else {
            float decimalNum = fracPart * 10.0;
            for(int i = 0; i < stepAmt - integerDigits; i++) {
                decimalNum = fract(decimalNum) * 10.0;
            }
            digitColor += drawSingleDigit(
                uv - (offset + stepSize * float(stepAmt + 1)), 
                int(decimalNum),
                color
            );
        }
    }
    
    return digitColor;
}

//========================================
// Variable Display Functions
//========================================

vec4 drawFloatDigits(vec2 uv, float scale, vec2 offset, float displayNumber, int integerDigits, int decimalPlaces, vec4 color) {
    vec2 thisOffset = vec2(offset.x / scale, - offset.y / scale);
    return getFloatDigitsColor(uv, scale, thisOffset, displayNumber, integerDigits, decimalPlaces, color);
}


vec4 drawVec2Digits(vec2 uv, float scale, vec2 offset, vec2 displayNumbers, int integerDigits, int decimalPlaces, vec4 color) {
    vec4 digitColor = vec4(0.0);
    for (int i = 0; i < 2; i++) {
        vec2 thisOffset = vec2(offset.x / scale, -float(i) * 3.0 - offset.y / scale);
        digitColor += getFloatDigitsColor(uv, scale, thisOffset, displayNumbers[i], integerDigits, decimalPlaces, color);
    }
    return digitColor;
}

vec4 drawVec4Digits(vec2 uv, float scale, vec2 offset, vec4 displayNumbers, int integerDigits, int decimalPlaces, vec4 color) {
    vec4 digitColor = vec4(0.0);
    for (int i = 0; i < 4; i++) {
        vec2 thisOffset = vec2(offset.x / scale, -float(i) * 3.0 - offset.y / scale);
        digitColor += getFloatDigitsColor(uv, scale, thisOffset, displayNumbers[i], integerDigits, decimalPlaces, color);
    }
    return digitColor;
}

vec4 drawCircle(vec2 uv, vec2 center, float size, vec4 color) {
    vec2 distanceFromCenter2d = (uv - center) * xyRatio;
    float distanceFromCenter = length(distanceFromCenter2d);
    float opacity = smoothstep(size, size * 0.75, distanceFromCenter) - 
                   smoothstep(size * 0.75, size * 0.6, distanceFromCenter);
    return color * opacity;
}

//========================================
// Main function
//========================================

void main() {
    // Setup and initialization
    vec4 originalImage = IMG_NORM_PIXEL(inputImage, isf_FragNormCoord);
    vec4 measureColor = IMG_NORM_PIXEL(inputImage, measurePoint);
    vec2 uv = normCoordToEquidistCoord(isf_FragNormCoord);
    
    // Display configuration
    float scale = 0.03;
    vec2 coordOffset = vec2(-0.4, -0.4);   // Coordinate display position
    vec2 colorOffset = vec2(-0.4, -0.15);  // Color values display position
    vec2 beatOffset = vec2(-0.4, 0.35);    // Beat display position

    // Build up display elements
    vec4 displayOutput = vec4(0);
    
    // Draw coordinates if enabled
    if (showCoordinates) {
        vec2 coordPoint;
        if (coordMode == 0) { // Screen space coordinates
            coordPoint = measurePoint * RENDERSIZE;
            coordPoint.y = RENDERSIZE.y - coordPoint.y;
            displayOutput += drawVec2Digits(uv, scale, coordOffset, coordPoint, 1, 0, textColor);
        } else if (coordMode == 1) { // UV coordinates
            displayOutput += drawVec2Digits(uv, scale, coordOffset, measurePoint, 1, 4, textColor);
        } else if (coordMode == 2) { // NDC coordinates
            displayOutput += drawVec2Digits(uv, scale, coordOffset, (measurePoint - 0.5) * 2.0, 1, 4, textColor);
        } else { // Equidistant coordinates
            displayOutput += drawVec2Digits(uv, scale, coordOffset, normCoordToEquidistCoord(measurePoint), 1, 4, textColor);
        }
    }
    
    // Draw color values and measurement point if enabled
    if (showColor) {
        displayOutput += drawVec4Digits(uv, scale, colorOffset, measureColor, 1, 4, textColor);
        displayOutput += drawCircle(isf_FragNormCoord, measurePoint, 0.015, textColor);
    }
    
    // Draw beat value if enabled
    if (showBeat) {
        displayOutput += drawFloatDigits(uv, scale, beatOffset, BEAT, 1, 2, textColor);
    }

    // Blend display elements with original image
    gl_FragColor = mix(originalImage, displayOutput, displayOutput.a);
}