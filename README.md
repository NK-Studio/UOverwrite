# UOverwrite

이 에셋은 Unity에서 파일을 덮어쓰는 기능을 제공합니다.

## 설치 방법
### Git UPM
UOverwrite 패키지를 설치하려면 다음 단계가 필요합니다.
1. Git이 설치되어 있는지 확인하십시오.
2. Package Manager를 오픈합니다.
3. +버튼을 클릭하고, Add package from git URL을 클릭합니다.
4. `https://github.com/NK-Studio/UOverwrite.git?path=/Assets/Plugins/UOverwrite` 를 입력하고 추가 버튼을 클릭하세요.
   
### Unity Package
[Releases](https://github.com/NK-Studio/UOverwrite/releases)에서 최신 버전의 패키지를 다운로드 받아 설치합니다.

## 사용법

동일한 이름을 가진 파일이 존재할 경우, 덮어쓰기를 할 것인지 물어보는 팝업이 나타납니다. 사용자가 덮어쓰기를 선택하면 파일이 덮어써집니다.
<img width="296" alt="image" src="https://github.com/user-attachments/assets/1587a59c-5dee-46e7-b884-b97f52c455da" />


## 지원 확장자

### 텍스처

- .png
- .jpg
- .jpeg
- .tga

### 오디오

- .wav
- .mp3
- .ogg

### 비디오

- .mp4

## 테스트
- 1개의 파일을 덮어쓰는 경우 : ✅
- 다수의 파일을 덮어쓰는 경우 : ✅

## 문제점

얼핏 문제없이 동작하는 것처럼 보이지만, 컨트롤 D를 통한 복제도 파일 덮어쓰기를 유발합니다.  
이 문제는 단기간 아이디어로는 어렵습니다... 혹시 좋은 아이디어가 있다면 PR을 주시면 감사하겠습니다.

