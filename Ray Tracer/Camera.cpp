#include "Camera.h"
#include <iostream>

Camera::Camera()
{
}  


Camera::~Camera()
{
}

void Camera::set(glm::vec3 p, glm::vec3 f)
{
	position = p;
	front = f;
	side = glm::normalize(glm::cross(front, glm::vec3(0.0f, 1.0f, 0.0f)));
}

void Camera::getInput(unsigned char key)
{
	float move = 0.5f;
	float x = 0.0f;
	float y = 0.0f;
	update = true;
	switch (key)
	{
	case 'w':
		position += move * front;
		break;
	case 's':
		position -= move * front;
		break;
	case 'a':
		position -= move * side;
		break;
	case 'd':
		position += move * side;
		break;
	case 'z':
		position.y -= move;
		break;
	case 'x':
		position.y += move;
		break;
	case 'i':
		y = 3.5f;
		break;
	case 'k':
		y = -3.5f;
		break;
	case 'j':
		x = -3.5f;
		break;
	case 'l':
		x = 3.5f;
		break;
	default:
		update = false;
		break;
	}
	if (update)
	{
		xRotation += x;
		yRotation += y;

		//Need to cap the y rotation or the screen flips once it hits +/- 90
		if (yRotation > 89)
		{
			yRotation = 89;
		}
		else if (yRotation < -89)
		{
			yRotation = -89;
		}

		glm::vec3 f;
		f.x = cos(glm::radians(xRotation)) * cos(glm::radians(yRotation));
		f.y = sin(glm::radians(yRotation));
		f.z = sin(glm::radians(xRotation)) * cos(glm::radians(yRotation));
		front = glm::normalize(f);
		side = glm::normalize(glm::cross(front, glm::vec3(0.0f, 1.0f, 0.0f)));

	}
}
