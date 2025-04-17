#include <stdint.h>

// CYarp������
enum CYarpErrorCode
{
	// client�����Ч
	InvalidHandle = -1,

	// �޴���
	NoError = 0,

	// ���ӵ�������ʧ��
	ConnectFailure = 1,

	// ���ӵ��������ѳ�ʱ
	ConnectTimeout = 2,

	// ���ӵ����������֤��ͨ��
	ConnectUnauthorized = 3,

	// ���ӱ��ܾ�(��Ȩ��ͨ��)
	ConnectForbid = 4,
};

// CYarp�ͻ���
typedef void* CYarpClient;

// ����쳣�ص�
typedef void (*CYarpTunnelErrorCallback)(
	// ��������
	char16_t* type,
	// ������Ϣ
	char16_t* message);

// ��ɻص�
typedef void (*CYarpCompletedCallback)(
	// ������
	CYarpErrorCode errorCode);


// �ͻ���ѡ��
struct CYarpClientOptions
{
	// CYarp������Uri
	// ֧��http��https�� ws��wss
	char16_t* ServerUri;
	// Ŀ�������Uri
	// ֧��http��https
	char16_t* TargetUri;
	// Ŀ���������UnixDomainSocket·��[��ѡ]
	char16_t* TargetUnixDomainSocket;
	// ��server��target�����ӳ�ʱʱ��������Ĭ��Ϊ5s
	int32_t ConnectTimeout;
	// ����������ص�[��ѡ]
	CYarpTunnelErrorCallback TunnelErrorCallback;
};

// �����ͻ���
// ��������ȷʱ����NULL
extern "C" CYarpClient CYarpClientCreate(
	// ѡ��
	CYarpClientOptions* options);

// ��������ʱ������ͷ
extern "C" enum CYarpErrorCode CYarpClientSetConnectHeader(
	// �ͻ���
	CYarpClient client,
	// ����ͷ����
	char16_t* headerName,
	// ����ͷ��ֵ
	char16_t* headerValue);

// �ͷſͻ���
extern "C" void CYarpClientFree(
	// �ͻ���
	CYarpClient client);

// ͬ����������
extern "C" enum CYarpErrorCode CYarpClientTransport(
	// �ͻ���
	CYarpClient client);

// �첽��������
extern "C" enum CYarpErrorCode CYarpClientTransportAsync(
	// �ͻ���
	CYarpClient client,
	// ������ɻص���ΪNull��ת��ͬ������
	CYarpCompletedCallback completedCallback);