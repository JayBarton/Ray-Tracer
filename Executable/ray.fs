#version 330 core


in vec3 fragPos;
in vec2 tc;

uniform vec3 center;
uniform vec3 eye;
uniform vec2 resolution;
uniform sampler2D tex;
uniform mat4 view;

uniform int theSize;
uniform int numberOfLights;
uniform int gammaCorrection;

int numberOfSpheres = 3;
int numberOfCubes = 3;

const int numberOfBounces = 6;

//
// GLSL textureless classic 3D noise "cnoise",
// with an RSL-style periodic variant "pnoise".
// Author:  Stefan Gustavson (stefan.gustavson@liu.se)
// Version: 2011-10-11
//
// Many thanks to Ian McEwan of Ashima Arts for the
// ideas for permutation and gradient selection.
//
// Copyright (c) 2011 Stefan Gustavson. All rights reserved.
// Distributed under the MIT license. See LICENSE file.
// https://github.com/ashima/webgl-noise
//

vec3 mod289(vec3 x)
{
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 mod289(vec4 x)
{
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 permute(vec4 x)
{
  return mod289(((x*34.0)+1.0)*x);
}

vec4 taylorInvSqrt(vec4 r)
{
  return 1.79284291400159 - 0.85373472095314 * r;
}

vec3 fade(vec3 t) {
  return t*t*t*(t*(t*6.0-15.0)+10.0);
}

// Classic Perlin noise
float cnoise(vec3 P)
{
  vec3 Pi0 = floor(P); // Integer part for indexing
  vec3 Pi1 = Pi0 + vec3(1.0); // Integer part + 1
  Pi0 = mod289(Pi0);
  Pi1 = mod289(Pi1);
  vec3 Pf0 = fract(P); // Fractional part for interpolation
  vec3 Pf1 = Pf0 - vec3(1.0); // Fractional part - 1.0
  vec4 ix = vec4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
  vec4 iy = vec4(Pi0.yy, Pi1.yy);
  vec4 iz0 = Pi0.zzzz;
  vec4 iz1 = Pi1.zzzz;

  vec4 ixy = permute(permute(ix) + iy);
  vec4 ixy0 = permute(ixy + iz0);
  vec4 ixy1 = permute(ixy + iz1);

  vec4 gx0 = ixy0 * (1.0 / 7.0);
  vec4 gy0 = fract(floor(gx0) * (1.0 / 7.0)) - 0.5;
  gx0 = fract(gx0);
  vec4 gz0 = vec4(0.5) - abs(gx0) - abs(gy0);
  vec4 sz0 = step(gz0, vec4(0.0));
  gx0 -= sz0 * (step(0.0, gx0) - 0.5);
  gy0 -= sz0 * (step(0.0, gy0) - 0.5);

  vec4 gx1 = ixy1 * (1.0 / 7.0);
  vec4 gy1 = fract(floor(gx1) * (1.0 / 7.0)) - 0.5;
  gx1 = fract(gx1);
  vec4 gz1 = vec4(0.5) - abs(gx1) - abs(gy1);
  vec4 sz1 = step(gz1, vec4(0.0));
  gx1 -= sz1 * (step(0.0, gx1) - 0.5);
  gy1 -= sz1 * (step(0.0, gy1) - 0.5);

  vec3 g000 = vec3(gx0.x,gy0.x,gz0.x);
  vec3 g100 = vec3(gx0.y,gy0.y,gz0.y);
  vec3 g010 = vec3(gx0.z,gy0.z,gz0.z);
  vec3 g110 = vec3(gx0.w,gy0.w,gz0.w);
  vec3 g001 = vec3(gx1.x,gy1.x,gz1.x);
  vec3 g101 = vec3(gx1.y,gy1.y,gz1.y);
  vec3 g011 = vec3(gx1.z,gy1.z,gz1.z);
  vec3 g111 = vec3(gx1.w,gy1.w,gz1.w);

  vec4 norm0 = taylorInvSqrt(vec4(dot(g000, g000), dot(g010, g010), dot(g100, g100), dot(g110, g110)));
  g000 *= norm0.x;
  g010 *= norm0.y;
  g100 *= norm0.z;
  g110 *= norm0.w;
  vec4 norm1 = taylorInvSqrt(vec4(dot(g001, g001), dot(g011, g011), dot(g101, g101), dot(g111, g111)));
  g001 *= norm1.x;
  g011 *= norm1.y;
  g101 *= norm1.z;
  g111 *= norm1.w;

  float n000 = dot(g000, Pf0);
  float n100 = dot(g100, vec3(Pf1.x, Pf0.yz));
  float n010 = dot(g010, vec3(Pf0.x, Pf1.y, Pf0.z));
  float n110 = dot(g110, vec3(Pf1.xy, Pf0.z));
  float n001 = dot(g001, vec3(Pf0.xy, Pf1.z));
  float n101 = dot(g101, vec3(Pf1.x, Pf0.y, Pf1.z));
  float n011 = dot(g011, vec3(Pf0.x, Pf1.yz));
  float n111 = dot(g111, Pf1);

  vec3 fade_xyz = fade(Pf0);
  vec4 n_z = mix(vec4(n000, n100, n010, n110), vec4(n001, n101, n011, n111), fade_xyz.z);
  vec2 n_yz = mix(n_z.xy, n_z.zw, fade_xyz.y);
  float n_xyz = mix(n_yz.x, n_yz.y, fade_xyz.x); 
  return 2.2 * n_xyz;
}

//End of Noise functions

struct Material
{
	vec3 color;
	float a; //ambient
	float d; //diffuse
	float s; //specular
	float n; //shininess
	float reflect;
	float refract;
	float eta;
	int tex;
};

struct Sphere
{
	vec3 position;
	Material material;
	float r;
};

struct Cube
{
	vec3 min;
	vec3 max;
	Material material;
};

struct Plane
{
	vec3 N;
	Material material;
};

struct Light
{
	vec3 position;
	vec3 color;
};

struct Ray 
{
	vec3 O;
	vec3 D;
};

struct Hit
{
	float t;
	vec3 N;
	Material material;
};


Light lights[3];

Sphere spheres[3];
Cube cubes[3];
Plane plane;

//if a quick and easy material is needed
Material simpleMaterial(vec3 color)
{
	return Material(color, 0.3, 0.7, 1, 100, 0, 0, 1, 0);
}

//For misses
Hit noHit = Hit(-1, vec3(0), simpleMaterial(vec3(0)));


Hit IntersectSphere(Ray ray, Sphere sphere)
{
	float A = dot(ray.D, ray.D);
	float B = 2 * (dot (ray.D, ray.O - sphere.position));
	float C = dot((ray.O - sphere.position), (ray.O - sphere.position)) - pow(sphere.r, 2);
	
	float t = 1000;
	vec3 N = vec3(0);
	
	float discriminant = (B*B - 4 * A*C);
	
	if (discriminant < 0)
	{
		return noHit;
	}
	else
	{
		float t0;
		float t1;
		t0 = (-B + sqrt(discriminant))/2*A;
		t1 = (-B - sqrt(discriminant))/2*A;
		t = t0;
		if (t > t1)
		{
			t = t1;
			if (t < 0)
			{
				t = t0;
			}
		}
		else
		{
			if (t < 0)
			{
				t = t1;
			}
		}
		if (t < 0)
		{
			return noHit;
		}
		N = ((ray.O + t*ray.D) - sphere.position)/sphere.r;
	}
	Hit hit = Hit(t, N, sphere.material);
	return hit;
}

Hit intersectPlane(Ray ray, Plane plane)
{
    float denominator = dot(plane.N, ray.D);
    if (denominator > 0) 
	{
		return noHit;
	}

    float t = -(dot(ray.O, plane.N)) / denominator;
	vec3 pos = (ray.O + t*ray.D);
	
	if(pos.x > 30.0 || pos.x < -30.0 || pos.z > 20 || pos.z < -20) 
	{
		return noHit;;
	}
	
    return Hit(t, plane.N, plane.material);
	
}

Hit intersectCube(Ray ray, Cube cube) 
{
	Hit hit = noHit;
	vec3 tMin = (cube.min - ray.O) / ray.D;
	vec3 tMax = (cube.max - ray.O) / ray.D;
	vec3 t1 = min(tMin, tMax);
	vec3 t2 = max(tMin, tMax);
	float near = max(max(t1.x, t1.y), t1.z); //negative if ray origin is infront of the cube
	float far = min(min(t2.x, t2.y), t2.z);
 
	if (near > 0.0 && near < far)
	{
		vec3 hitPosition = (ray.O + near*ray.D);
		vec3 N;
		if(abs(hitPosition.x - cube.min.x) < 0.01)
		{
			N = vec3(-1, 0, 0);
		}
		else if(abs(hitPosition.x - cube.max.x) < 0.01)
		{
			N = vec3(1, 0, 0);
		}
		else if(abs(hitPosition.y - cube.min.y) < 0.01)
		{
			N = vec3(0, -1, 0);
		}
		else if(abs(hitPosition.y - cube.max.y) < 0.01)
		{
			N = vec3(0, 1, 0);
		}
		else if(abs(hitPosition.z - cube.min.z) < 0.01)
		{
			N = vec3(0, 0, -1);
		}
		else if(abs(hitPosition.z - cube.max.z) < 0.01)
		{
			N = vec3(0, 0, 1);
		}
		hit = Hit(near, N, cube.material);
	}

	return hit;
}

Hit IntersectScene(Ray ray)
{
	Hit minHit = noHit;
	minHit = intersectPlane(ray, plane);
	int index = -1;
	for(int i = 0; i < numberOfSpheres; i ++)
	{
		Hit h = IntersectSphere(ray, spheres[i]);
		if(h.t >= 0 && (h.t < minHit.t || minHit.t == -1))
		{
			minHit = h;
		}
	}
	for(int i = 0; i < numberOfCubes; i ++)
	{
		Hit h = intersectCube(ray, cubes[i]);
		if(h.t >= 0 && (h.t < minHit.t || minHit.t == -1))
		{
			minHit = h;
		}
	}
	return minHit;
}

void textureHit(inout vec3 color, vec3 hitPosition, inout vec3 N, int mTex)
{
	if(mTex > 0)
	{
		//Checkerboard
		if(mTex == 1)
		{
			float tile = mod(floor(hitPosition.x) +  floor(hitPosition.z), 2);
		
			if(tile > 0)
			{
				color = vec3(0);
			}
			else
			{
				color = vec3(1);
			}
		}
		//Marble
		else if(mTex == 2)
		{
			vec4 veinColour = vec4(0.8, 0.8, 0.8, 1.0);

			vec4 baseColour = vec4(color, 1);
			float turb = abs(cnoise(2*hitPosition))*0.5
			+ abs(cnoise(4*hitPosition))*0.25
			+ abs(cnoise(8*hitPosition))*0.125
			+ abs(cnoise(16*hitPosition))*0.0625;
			float marble = 0.5*sin(6.28*(hitPosition.x + turb))+0.5;

			
			color = mix(veinColour, baseColour, marble).rgb;
		}
		//Earth
		else if(mTex == 3)
		{		
			vec2 uv;
			uv.x = 0.5 + atan(N.z, N.x) / 6.28;
			uv.y = 0.5 - asin(N.y) / 3.14;
			color = texture(tex, vec2(-uv.x , -uv.y )).rgb;
			
			if(color.b < 0.63)
			{
				vec3 n;
				n = vec3((cnoise(hitPosition*2)), (cnoise(hitPosition*4)), (cnoise(hitPosition *8)));
				N = normalize(N + n *2);
			}
		}
	}
}


vec3 getColor(Hit hit, Ray ray)
{	

	vec3 theColor = vec3(0);

	float ambient = hit.material.a;
	float diffuse = 0.0f;
	float specular = 0.0f;
	float shininess = hit.material.n;
	
	float t = hit.t;

	vec3 color = hit.material.color;
	vec3 hitPosition = (ray.O + t*ray.D);
	vec3 N = hit.N;
	N = normalize(N);
	int mTex = hit.material.tex;
		
	vec3 dsColor = vec3(0);
	bool test = true;
	//if(!test)
	{
		textureHit(color, hitPosition, N, mTex);
	}
	
	vec3 ambientColor = color * ambient;

	for(int i = 0; i < numberOfLights; i ++)
	{
		Light light = lights[i];
		bool shadow = false;
		
		Hit shadowHit = noHit;
		vec3 lightDirection = normalize(light.position - hitPosition);
		Ray toLight = Ray(hitPosition + 0.001 * lightDirection, lightDirection);
		

		for(int c = 0; c < numberOfSpheres; c++)
		{
			Hit s = IntersectSphere(toLight, spheres[c]);
			if(s.t >= 0 && (s.t < shadowHit.t || shadowHit.t == -1))
			{
				shadow = true;
				break;
			}
		}
		if(!shadow)
		{
			for(int c = 0; c < numberOfCubes; c++)
			{
				Hit s = intersectCube(toLight, cubes[c]);
				if(s.t >= 0 && (s.t < shadowHit.t || shadowHit.t == -1))
				{
					shadow = true;
					break;
				}
			}
		}
		if(!shadow)
		{
			vec3 L = light.position - hitPosition;
			vec3 H = normalize(L + ray.O);
			
			L = normalize(L);
			
			float diff = dot(N, L);
			float spec;
			if (diff < 0.0f)
			{
				diff = 0.0f;
				spec = 0.0f;
			}
			else
			{
				if(hit.material.s > 0)
				{
					//Phong specular reflection
					vec3 R = -L + 2 * (dot(L, N) * N);
					float specCalc = dot(normalize(ray.O),R);
					
					spec = pow(max(specCalc, 0.0f), shininess);
				}
			}
			diffuse = diff;
			specular = spec;
		
			dsColor += hit.material.d * color  * light.color * diffuse + hit.material.s * light.color * specular;
		}
	}

	theColor = ambientColor + dsColor;
	
	return theColor;
}

vec3 trace(Ray ray)
{  

	vec3 theColor = vec3(0);
	vec3 bgColor = vec3(0.39, 0.61, 0.94);
			
	Hit hit = IntersectScene(ray);	
	
	if(hit != noHit)
	{
		theColor = getColor(hit, ray);
		float frac = 1;
		for(int i = 0; i < numberOfBounces; i++)
		{
			vec3 hitPosition = (ray.O + hit.t*ray.D);

			vec3 N = hit.N;
			
			if(hit.material.reflect > 0 || hit.material.refract > 0) //wip
			{
				if(hit.material.reflect > 0)
				{
					frac *= hit.material.reflect;
					vec3 reflectDirection = normalize(reflect(ray.D, N));
					ray = Ray(hitPosition + (0.001 * reflectDirection), reflectDirection);
					hit = IntersectScene(ray);
					if(hit != noHit)
					{
						theColor += getColor(hit, ray) * frac;
					}
					else
					{
						theColor += bgColor *frac;
					}
				}
				else if(hit.material.refract > 0)
				{
					frac *= hit.material.refract;
					float n1;
					float n2;
					float cosI = dot(ray.D, N);
					if (dot(ray.D, N) > 0)
					{
						n1 = hit.material.eta;
						n2 = 1;
						N = -N;
					}
					else
					{
						n1 = 1;
						n2 = hit.material.eta;
					}

					float n = n1 / n2;

					float x = 1 - pow(n, 2) *(1 - pow(dot(ray.D, N), 2));
					if (x >= 0)
					{		
						vec3 refractDirection = refract(ray.D, N, n1/n2);
						ray = Ray(hitPosition + (0.001 * refractDirection), refractDirection);
						hit = IntersectScene(ray);
						if(hit != noHit)
						{
							theColor += getColor(hit, ray) * frac;
						}
						else
						{
							theColor += bgColor * frac;
						}
					}
					else
					{
						//total internal reflection
						vec3 reflectDirection = normalize(reflect(ray.D, N));
						ray = Ray(hitPosition + (0.001 * reflectDirection), reflectDirection);
						hit = IntersectScene(ray);
						if(hit != noHit)
						{
							theColor += getColor(hit, ray) * frac;
						}
						else
						{
							theColor += bgColor *frac;
						}
					}
				}
			}
			else
			{
				break;
			}
		}
	}
	else
	{
		theColor = bgColor;
	}
	
	return (theColor);
}

void main() 
{
    vec2 uv = gl_FragCoord.xy / resolution.xy - vec2(0.5);
    uv.x *= resolution.x / resolution.y;
	
	vec3 dir;
	Ray ray;
			
	spheres[0] = Sphere(center, Material(vec3(0.4, 0.0, 1.0), 0.3, 0.7, 1, 100, 0.1, 0, 1, 0), 1.0);
	spheres[1] = Sphere(vec3(0.0, 5.0, -10.0) , Material(vec3(0.0, 0.0, 1.0), 0.2, 1, 1, 8, 0.0, 0.0, 1, 3), 2.0);
	spheres[2] = Sphere(vec3(2, 5.0, -2.0), Material(vec3(0.0, 0.0, 0.0), 0.0, 0.0, 0.5, 32, 0.0, 0.9, 1.31, 0), 2.0);
	
	cubes[0] = Cube(vec3(-8, 10, -15), vec3(-4, 14, -11), Material(vec3(1.0, 0.0, 0.0), 0.3, 0.7, 1, 100, 0.0, 0, 1, 2));
	cubes[1] = Cube(vec3(-4, 2, -13), vec3(-2, 4, -8), simpleMaterial(vec3(0.0, 1.0, 0.0)));
	cubes[2] = Cube(vec3(-30, -1, -20), vec3(30, 18, -19.99), Material(vec3(1), 0.0, 0.0, 1, 100, 1.0, 0, 1, 0));
	
	plane = Plane(vec3(0, 1, 0),  Material(vec3(1), 0.02, 1, 0, 1, 0, 0, 1,  1));
	
	lights[0] = Light(vec3(0, 80, -30), vec3(0.4, 0.4, 0.4));
	lights[1] = Light(vec3(0, 80, 10), vec3(0.4, 0.4, 0.4));
	lights[2] = Light(vec3(20, 80, -10), vec3(0.4, 0.4, 0.4));
	
	int gridSize = theSize;
	vec3 col = vec3(0);
	if(gridSize == 1)
	{
		//no need to sample if there is only one ray.
		dir = normalize(vec3(uv.x, uv.y, -1.0));
		dir = (transpose(view) * vec4(dir, 1.0)).xyz;
		ray = Ray(eye, dir);
		col = trace(ray);
	}
	else
	{	
		//Grid sampling
		/*for(int i = 0; i < pow(gridSize, 2); i ++)
		{
			vec2 uv2;
			float x = 0;
			float y = 0;
			
			x =  abs(cnoise(gl_FragCoord.xyz + i)) /resolution.x;
			y =  abs(cnoise(gl_FragCoord.xyz + i * 2)) / resolution.y;
			
			uv2 = uv + vec2(x, y);

			dir = normalize(vec3(uv2.x, uv2.y, -1.0));
			dir = (transpose(view) * vec4(dir, 1.0)).xyz;
			ray = Ray(eye, dir);

			col += trace(ray);
		}*/
		//Random sampling
		/*for(int i = 0; i < gridSize; i ++)
		{
			for(int c = 0; c < gridSize; c ++)
			{
				float x = 0;
				float y = 0;
				x = c / resolution.x / gridSize;
				y = i / resolution.y / gridSize;
				
				vec2 uv2;
				//uv2 = (gl_FragCoord.xy + vec2(x, y)) / resolution.xy - vec2(0.5);

				uv2 = uv + vec2(x, y);

				dir = normalize(vec3(uv2.x, uv2.y, -1.0));
				dir = (transpose(view) * vec4(dir, 1.0)).xyz;
				ray = Ray(eye, dir);
				
				col += trace(ray);

			}
		}*/
		//Combined
		for(int i = 0; i < gridSize; i ++)
		{
			for(int c = 0; c < gridSize; c ++)
			{
				float x = 0;
				float y = 0;
				x = (c / resolution.x / gridSize) + cnoise(gl_FragCoord.xyz + i + c) /resolution.x / gridSize; 
				y = (i / resolution.y / gridSize) + cnoise(gl_FragCoord.xyz + i + c) / resolution.y / gridSize;
				
				vec2 uv2;
				//uv2 = (gl_FragCoord.xy + vec2(x, y)) / resolution.xy - vec2(0.5);

				uv2 = uv + vec2(x, y);

				dir = normalize(vec3(uv2.x, uv2.y, -1.0));
				dir = (transpose(view) * vec4(dir, 1.0)).xyz;
				ray = Ray(eye, dir);
				
				col += trace(ray);
			}
		}
		col /= pow(gridSize, 2);
	}
	
	if(gammaCorrection > 0)
	{
		gl_FragColor = vec4(pow(col, vec3(1.0/2.2)), 1.0);
	}
	else
	{
		gl_FragColor = vec4(col, 1.0); 
	}
}