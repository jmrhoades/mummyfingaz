/*{
	"DESCRIPTION": "2D Matrices",
	"CREDIT": "Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
		"TEST"
	],

	"INPUTS": [
		{
			"NAME": "size",
			"TYPE": "float",
			"DEFAULT": 0.1,
			"MIN": 0.0,
			"MAX": 0.5
		},
		{
			"NAME": "scale",
			"TYPE": "float",
			"DEFAULT": 1.0,
			"MIN": 0.0,
			"MAX": 2.0
		}
	]
}*/


#define PI 3.14159265359

//rotate function

mat2 rotate2d(float _angle){
    return mat2(cos(_angle),-sin(_angle),
                sin(_angle),cos(_angle));
}

mat2 scale(vec2 _scale){
    return mat2(_scale.x,0.0,
                0.0,_scale.y);
}



float box(in vec2 _st, in vec2 _size){
    _size = vec2(0.5) - _size*0.5;
    vec2 uv = smoothstep(_size, _size+vec2(0.001), _st);
    uv  *= smoothstep(_size, _size+vec2(0.001), vec2(1.0) - _st);
    return uv.x * uv.y;
}



float cross(in vec2 _st, float _size){
    return  box(_st, vec2(_size,_size/4.)) + 
            box(_st, vec2(_size/4.,_size));
}





void main(){
    vec2 st = gl_FragCoord.xy/RENDERSIZE;
    vec3 color = vec3(0.0);
    
    // move space from the center to the vec2(0.0)
    st -= vec2(0.5);
    // rotate the space
    st = rotate2d( sin(TIME)*PI ) * st;
    // move it back to the original place
    st = scale( vec2(abs(sin(TIME)+scale)) ) * st;
        
    st += vec2(0.5);

    // Show the coordinates of the space on the background
     color = vec3(st.x,st.y,0.0);

    // Add the shape on the foreground
    color += vec3(cross(st,size));

    gl_FragColor = vec4(color,1.0);
}


