/*
CSCI 4110 Course Assignment
Ray Tracer
Damon Barton
*/
#define GLM_FORCE_RADIANS
#define _USE_MATH_DEFINES
#include <Windows.h>
#include <GL/glew.h>
#include <gl/glut.h>
#include <GL/freeglut_ext.h>
#include <glm.hpp>
#include <gtc/matrix_transform.hpp>
#include <gtc/type_ptr.hpp>
#include <math.h>
#include <stdio.h>
#include "Shaders.h"
#include "texture.h"
#include <iostream>

#include "Camera.h"

Camera camera;

GLuint cubeProgram;

GLuint cubeVAO;
GLuint cubeTBuffer;

int cubeTraingles;
bool gamma = true;

glm::vec3 position;
float t = 0.0f;

int theSize = 1;
int numberOfLights = 1;

glm::vec2 resolution;

void cubeInit()
{
	GLuint vbuffer;
	GLuint ibuffer;
	GLint vPosition;

	glGenVertexArrays(1, &cubeVAO);
	glBindVertexArray(cubeVAO);
	GLfloat vertices[] = 
	{
		1.0f,  1.0f, 0.0f,
		1.0f, -1.0f, 0.0f,
		-1.0f, -1.0f, 0.0f,
		-1.0f,  1.0f, 0.0f
	};

	GLuint indices[] = {
		0, 1, 3, 
		1, 2, 3
	};

	cubeTraingles = 2;

	Texture * texture = loadTexture("earth.jpg");

	glGenTextures(1, &cubeTBuffer);
	glActiveTexture(GL_TEXTURE0);
	glBindTexture(GL_TEXTURE_2D, cubeTBuffer);
	glTexStorage2D(GL_TEXTURE_2D, 1, GL_RGBA8, texture->width, texture->height);
	glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, texture->width, texture->height, GL_RGB,GL_UNSIGNED_BYTE, texture->data);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
	
	glGenBuffers(1, &vbuffer);
	glBindBuffer(GL_ARRAY_BUFFER, vbuffer);
	glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

	glGenBuffers(1, &ibuffer);
	glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, ibuffer);
	glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_STATIC_DRAW);

	glUseProgram(cubeProgram);
	glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);
	glEnableVertexAttribArray(0);

}

void changeSize(int w, int h) {

	// Prevent a divide by zero, when window is too short
	// (you cant make a window of zero width).

	if (h == 0)
		h = 1;

	float ratio = 1.0 * w / h;

	glViewport(0, 0, w, h);

	resolution.x = glutGet(GLUT_WINDOW_WIDTH);
	resolution.y = glutGet(GLUT_WINDOW_HEIGHT);
}

void displayFunc(void)
{

	int cLoc;
	int eyeLoc;
	int resLoc;

	int viewLoc;

	int sizeLoc;
	int lightLoc;
	int gammaLoc;

	glm::mat4 view = camera.getMatrix();

	glClear(GL_COLOR_BUFFER_BIT);
	glUseProgram(cubeProgram);
	glBindTexture(GL_TEXTURE_CUBE_MAP, cubeTBuffer);

	cLoc = glGetUniformLocation(cubeProgram, "center");
	glUniform3f(cLoc, position.x, 10.0f, -10.0f);

	eyeLoc = glGetUniformLocation(cubeProgram, "eye");
	glm::vec3 e = camera.getPosition();
	glUniform3f(eyeLoc, e.x, e.y, e.z);

	resLoc = glGetUniformLocation(cubeProgram, "resolution");
	glUniform2f(resLoc, resolution.x, resolution.y);

	viewLoc = glGetUniformLocation(cubeProgram, "view");
	glUniformMatrix4fv(viewLoc, 1, 0, glm::value_ptr(view));

	sizeLoc = glGetUniformLocation(cubeProgram, "theSize");
	glUniform1i(sizeLoc, theSize);

	lightLoc = glGetUniformLocation(cubeProgram, "numberOfLights");
	glUniform1i(lightLoc, numberOfLights);

	gammaLoc = glGetUniformLocation(cubeProgram, "gammaCorrection");
	glUniform1i(gammaLoc, gamma);

	glDrawElements(GL_TRIANGLES, 3 * cubeTraingles, GL_UNSIGNED_INT, NULL);

	glutSwapBuffers();
}

void update()
{
	glm::vec3 start(-7.0f, 0.0f, -10.0f);
	glm::vec3 end(7.0f, 0.0f, -10.0f);
	position = (glm::mix(start, end, pow(sin(t), 2)));
	t += 0.000005f * GLUT_ELAPSED_TIME;
	glutPostRedisplay();

}

void keyboardFunc(unsigned char key, int x, int y) {

	camera.getInput(key);

	switch (key) {
	case '1':
		theSize++;
		if (theSize > 5)
		{
			theSize = 1;
		}
		std::cout << "Samples : " << pow(theSize, 2) << std::endl;
		break;

	case '2':
		numberOfLights++;
		if (numberOfLights > 3)
		{
			numberOfLights = 1;
		}
		std::cout << "Lights : " << numberOfLights << std::endl;
		break;
	case '3':
		if (gamma)
		{
			gamma = false;
			std::cout << "Gamma correction off" << std::endl;
		}
		else
		{
			gamma = true;
			std::cout << "Gamma correction on" << std::endl;
		}
		break;
	}

	glutPostRedisplay();

}

int main(int argc, char **argv) {
	int fs;
	int vs;

	position = glm::vec3(-7.0f, 0.0f, -10.0f);

	camera.set(glm::vec3(0.0f, 5.0f, 10.0f), glm::vec3(0.0f, 0.0f, -1.0f));

	glutInit(&argc, argv);

	glutInitDisplayMode(GLUT_DEPTH | GLUT_DOUBLE | GLUT_RGBA);
	glutInitWindowPosition(200, 0);
	glutInitWindowSize(600, 600);

	glutCreateWindow("raytracer");
	GLenum error = glewInit();
	if (error != GLEW_OK) {
		printf("Error starting GLEW: %s\n", glewGetErrorString(error));
		exit(0);
	}

	glutDisplayFunc(displayFunc);
	glutReshapeFunc(changeSize);
	glutKeyboardFunc(keyboardFunc);
	glutIdleFunc(update);

	glClearColor(1.0, 1.0, 1.0, 1.0);

	vs = buildShader(GL_VERTEX_SHADER, "ray.vs");
	fs = buildShader(GL_FRAGMENT_SHADER, "ray.fs");
	cubeProgram = buildProgram(vs, fs, 0);
	dumpProgram(cubeProgram, "Ray program");

	cubeInit();

	glutMainLoop();

}
